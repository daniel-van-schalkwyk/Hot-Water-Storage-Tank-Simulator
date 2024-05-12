function [] = SimulatorLite(T_profile_prev, T_amb, flowRate, T_inlet, coilPower)
%% Geyser Simulator LITE

h_ThermostatNorm
n_mix_discharge
n_mix_charge
% Get the current mass flow rate 
        massFlow_current = flowRate;
    
        % Get current ambient temp T_amb
        T_amb_current = T_amb;

        % Get the current inlet temperature
        T_inlet_current = T_inlet;

        % Get the current thermostat temp
        ThermostatPosIndex = ceil(h_ThermostatNorm*length(T_profile_prev));
        thermostatTemp = mean([T_profile_prev(ThermostatPosIndex), T_profile_prev(ThermostatPosIndex+1)]);

        % Check the state of flow: charging or discharging
        if(massFlow_current >= 0)
            flowState = 'discharge';
            n_mix = n_mix_discharge;
        elseif(massFlow_current < 0)
            flowState = 'charge';
            T_vec_current = flip(T_profile_prev, 1);
            massFlow_current = massFlow_current*-1;
            n_mix = n_mix_charge;
        else
            flowState = 'stationary';
        end

        if(n_mix == 0)
            n_mix = 1;
        end

        % Calculate the accurate layer masses and capacities for the current time iteration
        layerMasses = rho_w(T_vec_current + 273.15) .* tankGeomModel.layerVolumes;
        layerCapacities = layerMasses.*cp_w(T_vec_current + 273.15);

        %% Construct Matrix F
        F_mat = zeros(nodes, nodes);
        for layer = 1:1:nodes
            % Define useful annonymous functions
            c_v_Tprev = @(layer) cp_w(T_vec_current(layer-1) + 273.15);
            c_v_Tcurrent = @(layer) cp_w(T_vec_current(layer) + 273.15);
            c_v_Tnext = @(layer) cp_w(T_vec_current(layer+1) + 273.15);
            c_v_fracPrev = @(layer) c_v_Tprev(layer)/c_v_Tcurrent(layer);
            c_v_fracNext = @(layer) c_v_Tnext(layer)/c_v_Tcurrent(layer);

            hx_prevLayer = @(layer) U_layers(layer-1)*A_crossSec(layer-1)./layerCapacities(layer);
            hx_nextLayer = @(layer) U_layers(layer)*A_crossSec(layer)./layerCapacities(layer);
            hx_ambient = @(layer) U_amb(layer)*A_exposed(layer)./layerCapacities(layer);
            if(layer == 1)
                F_mat(layer, layer+1) = hx_nextLayer(layer) + c_v_fracNext(layer) * 1/delta_t_s*(layerMixPortions(layer));
                F_mat(layer, layer) = -(hx_ambient(layer) + hx_nextLayer(layer) + 1/delta_t_s*(layerMixPortions(layer)));
            elseif(layer == nodes)
                F_mat(layer, layer-1) = hx_prevLayer(layer) + c_v_fracPrev(layer)*1/delta_t_s*(layerMixPortions(layer-1));
                F_mat(layer, layer) = -(hx_ambient(layer) + hx_prevLayer(layer) + 1/delta_t_s*(layerMixPortions(layer-1)));
            else
                F_mat(layer, layer-1) = hx_prevLayer(layer) + c_v_fracPrev(layer)*1/delta_t_s*(layerMixPortions(layer-1));
                F_mat(layer, layer+1) = hx_nextLayer(layer) + c_v_fracNext(layer)*1/delta_t_s*(layerMixPortions(layer));
                F_mat(layer, layer) = -(hx_ambient(layer) + hx_nextLayer(layer) + hx_prevLayer(layer) + 1/delta_t_s*(layerMixPortions(layer) + layerMixPortions(layer-1)) );
            end
        end
        
        %% Construct input vector 
        coilState = Thermostat(thermostatTemp, setTemp, 3, prevCoilState);
        coilStates(time) = coilState;
        prevCoilState = coilState;
        u_input = [massFlow_current; massFlow_current*T_inlet_current; T_amb_current; coilState*Q_coil];

        %% Construct the G matrix
        % Construct the column vector for layer advection
        layerTransportCoeffs = zeros(nodes, 1);
        for layer = 1:1:nodes
            c_v_Tprev = @(layer) cp_w(T_vec_current(layer-1) + 273.15);
            c_v_Tcurrent = @(layer) cp_w(T_vec_current(layer) + 273.15);
            c_v_fracPrev = @(layer) c_v_Tprev(layer)/c_v_Tcurrent(layer);
            if(layer <= n_mix)
                if(layer == 1)
                    layerTransportCoeffs(layer) = layer/(n_mix*layerMasses(layer)) .* -T_vec_current(layer);
                else
                    layerTransportCoeffs(layer) = 1/(n_mix*layerMasses(layer)) .* ( (layer-1)*c_v_fracPrev(layer)*T_vec_current(layer-1) - layer*T_vec_current(layer));
                end
            else
                layerTransportCoeffs(layer) = 1/layerMasses(layer) .* (c_v_fracPrev(layer)*T_vec_current(layer-1) - T_vec_current(layer));
            end
        end

        % Construct the column vector for inlet water
        inletTransportCoeffs = zeros(nodes, 1);
        for layer = 1:1:n_mix
            inletTransportCoeffs(layer) = 1/(n_mix*layerMasses(layer));
        end
        
        % Construct the column vector for ambient coefficients
        ambCoeffs = U_amb.*A_exposed./layerCapacities;

        % Construct the column vector for coil input
        g_coeffs = g_coil./layerCapacities;

        % Construct the final G matrix
        G_mat = [layerTransportCoeffs, inletTransportCoeffs, ambCoeffs, g_coeffs];
    
        %% Calculate the change of temperature over time
        dTdt = F_mat*T_vec_current + G_mat*u_input;
        dTdt_mat(:, time) = dTdt;

        T_vec_next_temp = T_vec_current + delta_t_s*dTdt;

        % Calculate the next temperature vectors
        if(strcmp(flowState, 'charge'))
            % If in charge state, flip temperature vector back to normal
            T_vec_next(:, time) = flip(T_vec_next_temp, 1);
        else
            % Calculate the next temperature vectors
            T_vec_next(:, time) = T_vec_next_temp;
        end
        T_vec_current = T_vec_next(:, time);
end
