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
        Delta_t int32
        TimeSteps int32
        TankGeom
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
            obj = populatePhysicalParameters(obj);
            
            % Populate the geometric object with calculated geometric
            % parameters
            try
                obj.TankGeom = EwhGeometryTools.populateTankGeometry(obj.TankGeom);
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end

            % Populate the simulation parameters
            obj = populateSimParameters(obj);

            % Populate input parameters

            % Populate the events of the simulation
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
    
        function obj = populatePhysicalParameters(obj)
            try 
                obj.TankGeom.t = obj.ConfigJson.config.tWall;                               % Tank thickness [m]
                obj.TankGeom.L = obj.ConfigJson.config.longLength;                          % Length [m]
                obj.TankGeom.R = obj.ConfigJson.config.diameter/2 - obj.TankGeom.t;         % Radius (minus tank thickness) [m]
                obj.TankGeom.n = obj.ConfigJson.config.nodeNumber;
                obj.TankGeom.orientation = obj.ConfigJson.config.orientation;
                obj.TankGeom.layerConfig = obj.ConfigJson.config.layerConfig;
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end
    
        function obj = populateSimParameters(obj)
            
            try
                obj.SimStartTime = datetime(obj.ConfigJson.simParameters.startTime);
                obj.SimStopTime = datetime(obj.ConfigJson.simParameters.stopTime);
                obj.Delta_t = int32(obj.ConfigJson.simParameters.dt);
                obj.SimParams = obj.ConfigJson.simParameters;
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
            
            try
                obj.SimDuration = obj.SimStopTime - obj.SimStartTime;
                obj.TimeSteps = ceil(seconds(obj.SimDuration)/obj.Delta_t);
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end
    end
end