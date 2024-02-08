classdef EHWST_Simulator
    %UNTITLED Summary of this class goes here
    %   Detailed explanation goes here

    properties
        ConfigJson
        TankGeom
    end

    methods
        function obj = EHWST_Simulator(configJsonFileUrl)
            %EHWST_Simulator Construct an instance of this class
            %   Detailed explanation goes here
            % Open and read the configuration file
            try
                fid = fopen(configJsonFileUrl, 'r');
                str = fread(fid, '*char')';
                fclose(fid);
            catch exception
                fprintf('An error occurred: %s\n', exception.message);
                throw exception;
            end
            % Decode the configuration file
            try 
                obj.ConfigJson = jsondecode(str);
            catch exception
                fprintf('An error occurred: %s\n', exception.message);
                throw exception;
            end
            % Populate geometry object 
            obj.TankGeom.t = obj.ConfigJson.config.tWall;                               % Tank thickness [m]
            obj.TankGeom.L = obj.ConfigJson.config.longLength;                          % Length [m]
            obj.TankGeom.R = obj.ConfigJson.config.diameter/2 - TankGeom.t;             % Radius (minus tank thickness) [m]
            obj.TankGeom.n = obj.ConfigJson.config.nodeNumber;
            obj.TankGeom.orientation = obj.ConfigJson.config.orientation;
            obj.TankGeom.layerConfig = obj.ConfigJson.config.layerConfig;
            
            % Populate the geometric object 
            try
                populateTankGeometry(obj)
            catch ex
                fprintf('An error occurred: %s\n', ex.message);
                throw ex;
            end
        end
    end

    methods (Access = private)
        function tankGeometryStruct = populateTankGeometry(obj)
            tankGeom = obj.TankGeom;
            tankGeom.V = pi*tankGeom.R^2*tankGeom.L;     % Volume [m^3]
            % Calculated base on tankGeom input
            if(strcmp(tankGeom.orientation, 'V'))
                tankGeom.z_delta = tankGeom.L/tankGeom.n;
                tankGeom.h_layers = linspace(tankGeom.z_delta, tankGeom.L, tankGeom.n);
                tankGeom.h_sensors = tankGeom.h_layers - tankGeom.z_delta/2;
                % Determine the node volumes of the CHP buffer tank
                tankGeom.layerVolumes = getNodeVolumes(tankGeom);
            elseif(strcmp(tankGeom.orientation, 'H'))
                if(strcmp(tankGeom.layerConfig, 'CH'))
                    % In this case, volumes will vary while layer heights will remain
                    % constant
                    tankGeom.z_delta = 2*tankGeom.R/tankGeom.n;
                    tankGeom.h_layers = linspace(tankGeom.z_delta, 2*tankGeom.R, tankGeom.n);
                    tankGeom.h_sensors = tankGeom.h_layers - tankGeom.z_delta/2;
                    tankGeom.layerVolumes = getNodeVolumes(tankGeom);
                elseif(strcmp(tankGeom.layerConfig, 'CV'))
                    % In this case, the volume of each layer will be constant while the
                    % layer height will vary
                    tankGeom.layerVolumes = getNodeVolumes(tankGeom);
                    volVec = cumsum(tankGeom.layerVolumes);
                    h_layers = zeros(1, tankGeom.n);
                    for i = 1:1:tankGeom.n
                        [y] = GetThermoclineAndCentroids(tankGeom, volVec(i));
                        h_layers(i) = y.thermocline;
                    end
                    tankGeom.h_layers = h_layers;
                    tankGeom.z_delta = diff(h_layers);
                else
                    error('Unrecognised layer configuration');
                end
            else
                error('Unrecognised tank orientation');
            end
            tankGeometryStruct = tankGeom;
        end
    end
end