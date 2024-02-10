classdef EHWST_Simulator
    % EHWST_Simulator Construct an instance of this class 
            % The constructor of the simulation tool: Populates all the
            %   necessary properties ingested from the configuration file
            %   and executes fundemental methods to the simulation

    properties
        ConfigJson
        TankGeom
    end

    % Public methods
    methods
        function obj = EHWST_Simulator(configJsonFileUrl)
            % EHWST_Simulator Construct an instance of this class 
            % The constructor of the simulation tool: Populates all the
            %   necessary properties ingested from the configuration file
            %   and executes fundemental methods to the simulation

            % Open and read the configuration file
            try
                fid = fopen(configJsonFileUrl, 'r');
                str = fread(fid, '*char')';
                fclose(fid);
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw exception;
            end
            % Decode the configuration file
            try 
                obj.ConfigJson = jsondecode(str);
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
            catch
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end
    end
end