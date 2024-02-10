function [T_mat, dTdt_mat] = StateSpaceConvectionModel(tankGeomModel, simParams, inputs)
    %% Description:
    % This method envelopes the state space convection model and 
    % performs a complex sate space calculation using different heat transfer 
    % coefficients and other fitter parameters.
    % 
    % Arguments: 
    % tankGeomModel - R, L, V, n, h_sensors, z_delta
    % simParams - simTime_steps, delta_t_s, T_initial, U_amb, U_layers, n_mix_charge, n_mix_discharge
    % inputs - T_in, T_amb, m_flowrate
    %% Get all simulation parameters and inputs from provided structs
    try
        simTime_steps = simParams.simTime_steps;    % Total number of time steps required
        delta_t_s = simParams.delta_t_s;            % The change in time in seconds
        rho_w = simParams.rho_w;                    % An annonymous funtion for water density as a function of temp in Kelvin
        cp_w = simParams.cp_w;                      % An annonymous funtion for water specific heat capacity as a function of temp in Kelvin
        T_initial = simParams.T_initial;            % Initial layer temperatures, row vector
        U_amb = simParams.U_amb;                    % Heat transfer coefficients between layers and ambient
        U_layers = simParams.U_layers;              % Heat transfer coefficients between layers
        n_mix_charge = simParams.n_mix_charge;
        n_mix_discharge = simParams.n_mix_charge;
    catch
        error('Not all necessary parameters are provided in the "simParams" struct or field names do not match');
    end

    % Get all input data from argument struct
    try
        T_inlet = inputs.T_inlet;
        T_amb = inputs.T_amb;
        m_flowrate = inputs.m_flowrate;
    catch
        error('Not all necessary parameters are provided in the "inputs" struct or field names do not match');
    end

    % Assign number of nodes of model
    nodes = tankGeomModel.n;

    % Calculate the exposed surface are to the ambient 
    [A_exposed, valid] = getExposedSurfaceAreas(tankGeomModel);
    if(~valid)
        error('Validation for exposed surface area not satisfied')
    end

    % Calculate the cross-sectional areas of the layers
    A_crossSec = getLayerCrossSectionalAreas(tankGeomModel);
    
    % Get layer volumes
    layerVolumes = getNodeVolumes(tankGeomModel, 'horizontal', true);

    % Simulation run
    T_vec_next = zeros(nodes, simTime_steps);
    T_vec_current = T_initial;
    dTdt_mat = zeros(nodes, simTime_steps);
    for time = 1:1:simTime_steps
    
        % Get the current mass flow rate 
        massFlow_current = m_flowrate(time);
    
        % Get current ambient temp T_amb
        T_amb_current = T_amb(time);

        % Check the state of flow: charging or discharging
        if(massFlow_current >= 0)
            flowState = 'discharge';
            T_inlet_current = T_inlet(time, 1);
            n_mix = n_mix_discharge;
        elseif(massFlow_current < 0)
            flowState = 'charge';
            massFlow_current = massFlow_current*-1;
            T_vec_current = flip(T_vec_current, 1);
            T_inlet_current = T_inlet(time, 2);
            n_mix = n_mix_charge;
        else
            flowState = 'stationary';
        end

        % Calculate the accurate layer masses and capacities for the current time iteration
        layerMasses = rho_w(T_vec_current + 273.15) .* layerVolumes;
        layerCapacities = layerMasses.*cp_w(T_vec_current + 273.15);

        %% Construct Matrix F
        F_mat = zeros(nodes, nodes);
        for layer = 1:1:nodes
            hx_prevLayer = @(layer) U_layers(layer-1)*A_crossSec(layer-1)./layerCapacities(layer);
            hx_nextLayer = @(layer) U_layers(layer)*A_crossSec(layer)./layerCapacities(layer);
            hx_ambient = @(layer) U_amb(layer)*A_exposed(layer)./layerCapacities(layer);
            if(layer == 1)
                F_mat(layer, layer+1) = hx_nextLayer(layer);
                F_mat(layer, layer) = -(hx_ambient(layer) + hx_nextLayer(layer));
            elseif(layer == nodes)
                F_mat(layer, layer-1) = hx_prevLayer(layer);
                F_mat(layer, layer) = -(hx_ambient(layer) + hx_prevLayer(layer));
            else
                F_mat(layer, layer-1) = hx_prevLayer(layer);
                F_mat(layer, layer+1) = hx_nextLayer(layer);
                F_mat(layer, layer) = -(hx_ambient(layer) + hx_nextLayer(layer) + hx_prevLayer(layer));
            end
        end
        
        %% Construct input vector 
        u_input = [massFlow_current; massFlow_current*T_inlet_current; T_amb_current];

        %% Construct the G matrix
        % Construct the column vector for layer advection
        layerTransportCoeffs = zeros(nodes, 1);
        for layer = 1:1:nodes
            if(layer <= n_mix)
                if(layer == 1)
                    layerTransportCoeffs(layer) = layer/(n_mix*layerMasses(layer)) .* -T_vec_current(layer);
                else
                    layerTransportCoeffs(layer) = 1/(n_mix*layerMasses(layer)) .* ( (layer-1)*T_vec_current(layer-1) - layer*T_vec_current(layer));
                end
            else
                layerTransportCoeffs(layer) = 1/layerMasses(layer) .* (T_vec_current(layer-1) - T_vec_current(layer));
            end
        end

        % Construct the column vector for inlet water
        inletTransportCoeffs = zeros(nodes, 1);
        for layer = 1:1:n_mix
            inletTransportCoeffs(layer) = 1/(n_mix*layerMasses(layer));
        end
        
        % Construct the column vector for ambient coefficients
        ambCoeffs = U_amb.*A_exposed./layerCapacities;

        % Construct the final G matrix
        G_mat = [layerTransportCoeffs, inletTransportCoeffs, ambCoeffs];
    
        %% Calculate the change of temperature over time
        dTdt = F_mat*T_vec_current + G_mat*u_input;
        dTdt_mat(:, time) = dTdt;

        % Calculate the next temperature vectors
        if(strcmp(flowState, 'charge'))
            % If in charge state, flip temperature vector back to normal
            T_vec_next(:, time) = flip((T_vec_current + delta_t_s*dTdt), 1);
        else
            % Calculate the next temperature vectors
            T_vec_next(:, time) = T_vec_current + delta_t_s*dTdt;
        end
        T_vec_current = T_vec_next(:, time);
    end
    dTdt_mat = dTdt_mat';
    T_mat = T_vec_next';
end