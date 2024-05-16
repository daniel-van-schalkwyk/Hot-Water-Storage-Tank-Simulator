classdef EwhGeometryTools
    %UNTITLED Summary of this class goes here
    %   Detailed explanation goes here

    %% Static methods
    methods (Static)
        
        function tankGeometryStruct = populateTankGeometry(tankGeom)
            tankGeom.V = pi*tankGeom.R^2*tankGeom.L;     % Volume [m^3]
            % Calculated base on tankGeom input
            if(strcmp(tankGeom.orientation, 'V'))
                tankGeom.z_delta = tankGeom.L/tankGeom.n;
                tankGeom.h_layers = linspace(tankGeom.z_delta, tankGeom.L, tankGeom.n);
                tankGeom.h_sensors = tankGeom.h_layers - tankGeom.z_delta/2;
                tankGeom.h_sensorsNorm = tankGeom.h_sensors/(2*tankGeom.R);
                % Determine the node volumes of the CHP buffer tank
                tankGeom.layerVolumes = EwhGeometryTools.getNodeVolumes(tankGeom);
            elseif(strcmp(tankGeom.orientation, 'H'))
                if(strcmp(tankGeom.layerConfig, 'CH'))
                    % In this case, volumes will vary while layer heights will remain
                    % constant
                    tankGeom.z_delta = 2*tankGeom.R/tankGeom.n;
                    tankGeom.h_layers = linspace(tankGeom.z_delta, 2*tankGeom.R, tankGeom.n);
                    tankGeom.h_sensors = tankGeom.h_layers - tankGeom.z_delta/2;
                    tankGeom.h_sensorsNorm = tankGeom.h_sensors/(2*tankGeom.R);
                    tankGeom.layerVolumes = EwhGeometryTools.getNodeVolumes(tankGeom);
                elseif(strcmp(tankGeom.layerConfig, 'CV'))
                    % In this case, the volume of each layer will be constant while the
                    % layer height will vary
                    tankGeom.layerVolumes = EwhGeometryTools.getNodeVolumes(tankGeom);
                    volVec = cumsum(tankGeom.layerVolumes);
                    h_layers = zeros(1, tankGeom.n);
                    for i = 1:1:tankGeom.n
                        [y] = EwhGeometryTools.getThermoclineAndCentroids(tankGeom, volVec(i));
                        h_layers(i) = y.thermocline;
                    end
                    tankGeom.h_layers = h_layers;
                    tankGeom.z_delta = diff(h_layers);
                    tankGeom.h_sensorsNorm = tankGeom.h_layers/(2*tankGeom.R);
                else
                    error('Unrecognised layer configuration');
                end
            else
                error('Unrecognised tank orientation');
            end

            % Calculate and populate the exposed surface areas
            tankGeom.A_exposed = EwhGeometryTools.getExposedSurfaceAreas(tankGeom);

            % Calculate and populate the cross-sectional surface areas
            % between layers
            tankGeom.A_crossSec = EwhGeometryTools.getLayerCrossSectionalAreas(tankGeom);

            % Return fully populated geometrical model
            tankGeometryStruct = tankGeom;
        end

        function volumes = getNodeVolumes(tankGeom)
            % Set default input variables
           
            if(strcmp(tankGeom.layerConfig, 'CV'))
                volumes = zeros(tankGeom.n, 1) + pi*tankGeom.R^2*tankGeom.L/tankGeom.n;
                return;              
            end
        
            if(strcmp(tankGeom.layerConfig, 'CH'))
                nodeNumber = tankGeom.n;
                orientation = tankGeom.orientation;
                % Get node volumes
                if(orientation == "H")
                    % If geomerty is horizontal
                    V_cold_func = @(h) tankGeom.L .* ( tankGeom.R^2*acos((tankGeom.R - h)/tankGeom.R) - (tankGeom.R-h).*sqrt(2*tankGeom.R*h - h.^2));
                    volumes = zeros(nodeNumber, 1);
                    layerHeightsEqual = tankGeom.R*2/nodeNumber;
                    h_currentLayer = 0;
                    for i = 1:1:length(volumes)
                        h_prevLayer = h_currentLayer;
                        h_currentLayer = h_currentLayer + layerHeightsEqual;
                        volumes(i) = V_cold_func(h_currentLayer) - V_cold_func(h_prevLayer);
                    end
                % If orientation is vertical
                elseif(orientation == "V")
                    layerHeightsEqual = tankGeom.L/nodeNumber;
                    volumes = zeros(nodeNumber, 1) + pi*tankGeom.R^2*layerHeightsEqual;
                end
                % Return the real component
                volumes = real(volumes);
                return;
            end
            % If this context is reached, then an error occurred
            error("layerConfig parameter not recognised")
        end           
    
        function [A_exposed, valid] = getExposedSurfaceAreas(tankGeom, plotAreas)
            if(nargin < 2)
                plotAreas = false;
            end
            R = tankGeom.R;
            
            % If the tank is horizontally orientated
            if(tankGeom.orientation == 'H')
                [arcLengthPerLayer, valid] = EwhGeometryTools.getArcSegmentsPerLayer(tankGeom.h_layers, R);
                if(~valid)
                    error('Circular perimeter does not equal sum of layer arc lengths')
                end
            
                % Calculate the surface areas
                layerVolumes = tankGeom.layerVolumes;
                layerCircAreas = layerVolumes./tankGeom.L;
                valid = abs(pi*tankGeom.R^2 - sum(layerCircAreas)) < 1e-8;
                if(~valid)
                    error('BC not met: Full circular area does not equal sum of layer areas on circular face')
                end
                
                % Calculate full surface area of layer exposed to environment
                A_exposed = 2*layerCircAreas + arcLengthPerLayer'*tankGeom.L;
        
            % If the tank is Vertically orientated
            elseif(tankGeom.orientation == 'V')
                z_delta_vertical = tankGeom.L/tankGeom.n;
                A_exposed_vertical_long = zeros(tankGeom.n, 1) + 2*pi*tankGeom.R*z_delta_vertical;
                A_exposed_vertical_ends = zeros(tankGeom.n, 1);
                A_exposed_vertical_ends([1, end]) = pi*tankGeom.R^2;
                A_exposed = A_exposed_vertical_long + A_exposed_vertical_ends;
            end
            
            fullCylinderArea = 2*(pi*tankGeom.R^2) + 2*pi*tankGeom.R*tankGeom.L;
                % Check if sanity check of boundary condition is met
                valid = abs(fullCylinderArea - sum(A_exposed)) < 1e-8;
                if(~valid)
                    error('BC not met: Full cylindrical area does not equal sum of exposed surface areas of the layers')
                end
            
            % Plot the results if requested
            if(plotAreas)
                figure("Name", 'A_exposed')
                bar(A_exposed)
                xlabel('Layer number');
                ylabel('Exposed surface Area [m^2]');
                grid on;
            end
        end

        function [abs_arcLengths, valid] = getArcSegmentsPerLayer(h_layers, R)

            theta_h = @(h, R) 2*R*asin((sqrt(h.*(2*R - h)))/R);
            nrOfLayers = length(h_layers);
            midNode = ceil(nrOfLayers/2);
            arcLengthPerLayer = zeros(1, nrOfLayers); 
            if(mod(nrOfLayers, 2) ~= 0)
                for i = 1:1:nrOfLayers
                    if(i == 1)
                        arcLengthPerLayer(i) = theta_h(h_layers(i), R);
                    elseif(i == midNode)
                        arcLengthPerLayer(i) = 2*(theta_h(R, R) - theta_h(h_layers(i-1), R));
                    elseif(i > midNode)
                        arcLengthPerLayer(i) = arcLengthPerLayer(midNode-(i-midNode));
                    else
                        arcLengthPerLayer(i) = theta_h(h_layers(i), R) - theta_h(h_layers(i-1), R);
                    end
                end
            else
                for i = 1:1:nrOfLayers
                    if(i == 1)
                        arcLengthPerLayer(i) = theta_h(h_layers(i), R);
                    else
                        arcLengthPerLayer(i) = theta_h(h_layers(i), R) - theta_h(h_layers(i-1), R);
                    end
                end
            end
            abs_arcLengths = abs(arcLengthPerLayer);
            p_circ = 2*pi*R;
            p_all_layers = sum(abs_arcLengths);
            valid = abs(p_circ - p_all_layers) < 1e-8;
        end
    
        function [A_c] = getLayerCrossSectionalAreas(tankGeom)
        
            % If the tank is horizontally orientated
            if(tankGeom.orientation == 'H')
                A_crossSecFunc = @(h, R, L) 2*sqrt(h.*(2*R - h)) * L;
                tankGeom.A_crossSec = A_crossSecFunc(tankGeom.h_layers, tankGeom.R, tankGeom.L);
                A_c = tankGeom.A_crossSec;
        
            % If the tank is vertically orientated
            elseif(tankGeom.orientation == 'V')
                A_c = zeros(1, length(tankGeom.h_layers)) + pi*tankGeom.R^2;
            end
        end

        function [yLengths, Vhot, A_therm] = getThermoclineAndCentroids(tankGeom, V_cold)
            % Description of function goes here
        
            if(V_cold > tankGeom.V)
                error("Error: Provided cold volume exceeds total volume");
            elseif(V_cold < 0)
                error("Error: Provided cold volume is negative");
            end
            R = tankGeom.R;
            V_tank = tankGeom.V;
            L = tankGeom.V/(pi*R^2);
            Vhot = V_tank - V_cold;
            % Evaluate if V_cold is more or less than half of tank
            if(V_cold > V_tank/2)
                mode = "top";
            elseif(V_cold == V_tank/2)
                mode = "mid";
            else
                mode = "bot";
            end
            
            % Bisection method
            V_cold_func = @(h) L .* ( R^2*acos((R - h)/R) - (R-h).*sqrt(2*R*h - h.^2)) - V_cold;
            h = EwhGeometryTools.getRootWithBisectionMethod(V_cold_func, 0, 2*R, 1e-9);
        
            % Determine thermocline and report h
            yLengths.thermocline = h;
            if(mode == "top")
                h = 2*R - h;
        %     elseif(mode == "bot")
        %         h = h;
            end
            yLengths.h = h;
        
            % Determine centroids
            alpha_func = @(h) acos((R-h)/R);
            y_seg = @(al) (4*R*(sin(al))^3)/( 3*(2*al - sin(2*al)) );
            A_seg = @(al) 0.5*R^2*(2*al - sin(2*al));
            y_comp = @(al) (y_seg(al) * A_seg(al))/(pi*R^2 - A_seg(al));
            
            if(mode == "top")
                yLengths.y_cold = R - abs(y_comp(alpha_func(h)));
                yLengths.y_hot = R + abs(y_seg(alpha_func(h)));
            elseif(mode == "bot")
                yLengths.y_cold = R - abs(y_seg(alpha_func(h)));
                yLengths.y_hot = R + abs(y_comp(alpha_func(h)));
            end
        
            % Determine cross-sectional area of thermocline
            A_therm = 2*sqrt(h*(2*R - h)) * L;
        end

        % Bisection Method
        function [root] = getRootWithBisectionMethod(func, min, max, tol)
            % Define search window
            rootMin = min;
            rootMax = max;
        
            % Initiate loop
            counter = 0;
            result_est = mean([min, max]);
            while(abs(result_est) > tol)
                if(counter >= 100)                   % Check break condition
                    break;
                end
                
                root_est = mean([rootMin, rootMax]);   % Calculate new midsection
                result_est = func(root_est);        % Calculate result based on new midsection
                if(result_est == 0 || abs(rootMax-rootMin) < tol)
                    % Result found
                    break
                end
        
                if(sign(result_est) == -1)
                    rootMin = root_est;             % Max search window changes
                elseif(sign(result_est) == 1)
                    rootMax = root_est; 
                end
                counter = counter + 1;
            end
            root = root_est;                        % Assign new estimated root
        end
    end

    %% Private methods
    methods(Access = private)
        

        
    end

end