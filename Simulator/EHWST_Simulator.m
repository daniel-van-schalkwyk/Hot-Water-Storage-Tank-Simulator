classdef EHWST_Simulator
    % EHWST_Simulator Construct an instance of this class 
            % The constructor of the simulation tool: Populates all the
            %   necessary properties ingested from the configuration file
            %   and executes fundemental methods to the simulation

    properties
        ConfigJson struct
        SimParams struct
        SimStartTime datetime
        SimStopTime datetime
        SimDuration duration
        TimeVector datetime
        Delta_t int32
        TimeSteps int32
        TankGeom
        Profiles simProfiles
    end

    % Public methods
    methods (Access = public)
        function obj = EHWST_Simulator(configJson)
            % EHWST_Simulator Construct an instance of this class 
            % The constructor of the simulation tool: Populates all the
            %   necessary properties ingested from the configuration file
            %   and executes fundemental methods to the simulation

            try 
                obj.ConfigJson = configJson;
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw exception;
            end
            
            % Populate geometry object with physial parameters of tank
            obj = populateGeyserCharacteristics(obj);
            
            % Populate the geometric object with calculated geometric
            % parameters
            try
                obj.TankGeom = EwhGeometryTools.populateTankGeometry(obj.TankGeom);
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end

            % Populate the simulation parameters
            obj = populateModelParameters(obj);

            if(configJson.input.source.csv == true)
                obj.TimeVector = datetime(configJson.profiles.time.values, 'InputFormat', 'yyyy-MM-dd''T''HH:mm:ss');
            else
                obj.TimeVector = obj.SimStartTime:duration(0, 0, obj.Delta_t):obj.SimStopTime;
            end

            % Populate timesteps
            obj.TimeSteps = length(obj.TimeVector);

            % Extract input profiles from config file
            try
                obj.Profiles = simProfiles(configJson.profiles);
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end

        %% Call Generic State-Space Model
        function [T_mat_sim, dTdt_mat, coilStates] = simulate(obj)

            % Prepare arguments for model
            tankGeomModel = obj.TankGeom;
            inputs.T_inlet = obj.Profiles.inletTemp.values;
            inputs.T_amb = obj.Profiles.ambientTemp.values;

            if(strcmp(obj.Profiles.flowRate.unit, "L/min") || strcmp(obj.Profiles.flowRate.unit, "l/min"))
                inputs.flowrate = obj.Profiles.flowRate.values/60/1000;
            elseif(strcmp(obj.Profiles.flowRate.unit, "L/s") || strcmp(obj.Profiles.flowRate.unit, "l/s"))
                inputs.flowrate = obj.Profiles.flowRate.values/1000;
            elseif(strcmp(obj.Profiles.flowRate.unit, "m^3/s"))
                inputs.flowrate = obj.Profiles.flowRate.values;
            else
%                 throw(MException('flowRate::WrongFlowRate', 'unit for flow rate profile not recogninised'));
                error('unit for flow rate profile not recogninised');
            end
            
            nodes = obj.TankGeom.n;
            simParams.simTime_steps = obj.TimeSteps;    
            simParams.delta_t_s = obj.Delta_t;            
            simParams.rho_w = @(T) (1.49343e-3 - 3.7164e-6*T + 7.09782e-9*T.^2 - 1.90321e-20*T.^6).^-1;                    
            simParams.cp_w = @(T) 8.15599e3 - 2.80627*10*T + 5.11283e-2*T.^2 - 2.17582e-13*T.^6;                     
            simParams.T_initial = zeros(obj.TankGeom.n, 1) + obj.SimParams.tempInit;            
            simParams.U_amb = zeros(tankGeomModel.n, 1) + 2;
            simParams.U_layers = zeros(nodes-1, 1) + 5;
            simParams.n_mix_charge = 1;
            simParams.n_mix_discharge = 1;
            simParams.U_layers([1:simParams.n_mix_discharge, end-simParams.n_mix_charge:end]) = 10;
            simParams.layerMixPortions = zeros(nodes-1, 1);
            simParams.layerMixPortions([1:1, end-1:end]) = 0;
            simParams.eHeatingPower = obj.ConfigJson.input.coilPower.value;
            simParams.gCoeffs = [
                                            0.073178074
                                            0.123146458
                                            0.150144593
                                            0.161594474
                                            0.161867056
                                            0.145951432
                                            0.108614193
                                            0
                                            0
                                                ];
            simParams.h_ThermostatNorm = 0.43;
            simParams.setTemp = 50;

            % Call the main generic state-space function with prepared
            % inputs
            [T_mat_sim, dTdt_mat, coilStates] = StateSpaceConvectionMixingModel(tankGeomModel, simParams, inputs);
        end

        %% Some Geometric figures
        function figures = generateGeometricPlots(obj)
            
            tankGeomModel = obj.TankGeom;

            % Plot the cross-sectional area of the adjacent layers
            A_crossSecFig = figure;
            b = bar(tankGeomModel.A_crossSec(1:end-1)', LineWidth=1);
            b(1).FaceColor = 'k'; 
            b(1).FaceAlpha = 0.7; 
            xlabel("Layer interfaces");
            ylabel("Cross-sectional area [m^2]");
            customLabels = {'0-1', '1-2', '2-3', '3-4', '4-5', '5-6', '6-7', '7-8'}; 
            xticklabels(customLabels);
            
            figure
            b = bar(tankGeomModel.A_exposed, LineWidth=1);
            b(1).FaceColor = 'k'; 
            b(1).FaceAlpha = 0.7; 
            ylabel("Exposed surface area [m^2]");
            xlabel("Layer number")
            customLabels = {'0', '1', '2', '3', '4', '5', '6', '7', '8'};
            xticklabels(customLabels);
            
            figure
            b = bar(tankGeomModel.layerVolumes*1000, LineWidth=1);
            b(1).FaceColor = 'k'; 
            b(1).FaceAlpha = 0.7; 
            ylabel("Volume [l]");
            xlabel("Layer number")
            customLabels = {'0', '1', '2', '3', '4', '5', '6', '7', '8'};
            xticklabels(customLabels);

            figures = {A_crossSecFig};
        end

        function createAnimation(obj, urlDestination)
            v = VideoWriter(urlDestination);
            v.FrameRate = 24; % Set the frame rate to 24 frames per second
            open(v);
            for t = 1:1:obj.TimeSteps
                % -- Create plot here --

                % Get the current plot frame
                frame = getframe(gcf);
                % Write the video frame to the stream
                writeVideo(v, frame);
            end
            close(v);

        end

        function visualiseInputProfiles(obj)
            % Plot inlet flow rate and temp
            figure("Name", "Flow rate and inlet temp")
            subplot(2, 1, 1)
            plot(obj.TimeVector, obj.Profiles.flowRate.values);
            xlabel("Time");
            ylabel(sprintf('Flow rate [%s]', obj.Profiles.flowRate.unit))
            subplot(2, 1, 2)
            plot(obj.TimeVector, obj.Profiles.inletTemp.values);
            xlabel("Time");
            ylabel(sprintf('Inlet Temp [%s]', obj.Profiles.inletTemp.unit))

            % Plot ambient and set temp
            figure("Name", "Ambient and tank set temp")
            subplot(2, 1, 1)
            plot(obj.TimeVector, obj.Profiles.setTemp.values);
            xlabel("Time");
            ylabel(sprintf('Set Temp [%s]', obj.Profiles.setTemp.unit))
            subplot(2, 1, 2)
            plot(obj.TimeVector, obj.Profiles.ambientTemp.values);
            xlabel("Time");
            ylabel(sprintf('Inlet Temp [%s]', obj.Profiles.ambientTemp.unit))

        end
    end

    % Private methods
    methods (Access = private)
        function ConfigJson = readConfigFileIntoJson(urlString)
            try
                fid = fopen(urlString, 'r');
                str = fread(fid, '*char')';
                fclose(fid);
            catch exception
                fprintf('An error occurred: %s\n', exception.message);
                throw exception;
            end
            % Decode the configuration file
            try 
                ConfigJson = jsondecode(str);
            catch exception
                fprintf('An error occurred: %s\n', exception.message);
                throw exception;
            end
        end
    
        function obj = populateGeyserCharacteristics(obj)
            try 
                obj.TankGeom.t = obj.ConfigJson.geyser.tWall;                               % Tank thickness [m]
                obj.TankGeom.L = obj.ConfigJson.geyser.longLength;                          % Length [m]
                obj.TankGeom.R = obj.ConfigJson.geyser.diameter/2 - obj.TankGeom.t;         % Radius (minus tank thickness) [m]
                obj.TankGeom.n = obj.ConfigJson.modelParameters.nodeNumber;
                obj.TankGeom.orientation = obj.ConfigJson.geyser.orientation;
                obj.TankGeom.layerConfig = obj.ConfigJson.geyser.layerConfig;
                obj.TankGeom.h_thermostat_rel = obj.ConfigJson.geyser.h_thermistor_rel;
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end
    
        function obj = populateModelParameters(obj)
            
            try
                obj.SimStartTime = datetime(obj.ConfigJson.modelParameters.startTime);
                obj.SimStopTime = datetime(obj.ConfigJson.modelParameters.stopTime);
                obj.Delta_t = int32(obj.ConfigJson.modelParameters.dt);
                obj.SimParams = obj.ConfigJson.modelParameters;
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
            
            try
                obj.SimDuration = obj.SimStopTime - obj.SimStartTime;
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end
    end
end