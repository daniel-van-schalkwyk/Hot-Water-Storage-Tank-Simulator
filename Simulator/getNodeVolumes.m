function volumes = getNodeVolumes(tankGeom)
    % Set default input variables
    orientation = tankGeom.orientation;
    if(strcmp(tankGeom.layerConfig, 'CH'))
        equiDistantFlag = true;
    elseif(strcmp(tankGeom.layerConfig, 'CV'))
        volumes = zeros(tankGeom.n, 1) + pi*tankGeom.R^2*tankGeom.L/tankGeom.n;
        return;
    end

    nodeNumber = tankGeom.n;
    % Get node volumes
    if(orientation == "horizontal")
        % If geomerty is horizontal
        V_cold_func = @(h) tankGeom.L .* ( tankGeom.R^2*acos((tankGeom.R - h)/tankGeom.R) - (tankGeom.R-h).*sqrt(2*tankGeom.R*h - h.^2));
        if(equiDistantFlag)
            volumes = zeros(nodeNumber, 1);
            layerHeightsEqual = tankGeom.R*2/nodeNumber;
            h_currentLayer = 0;
            for i = 1:1:length(volumes)
                h_prevLayer = h_currentLayer;
                h_currentLayer = h_currentLayer + layerHeightsEqual;
                volumes(i) = V_cold_func(h_currentLayer) - V_cold_func(h_prevLayer);
            end
        else
            volumes = zeros(nodeNumber, 1);
            h_layers = flip(tankGeom.h_layers);
            for i = 1:1:length(volumes)
                if(i == 1)
                    volumes(i) = V_cold_func(h_layers(i));
                    continue;
                end
                volumes(i) = V_cold_func(h_layers(i)) - V_cold_func(h_layers(i-1));
            end
        end
    % If orientation is vertical
    elseif(orientation == "vertical")
        layerHeightsEqual = tankGeom.L/nodeNumber;
        volumes = zeros(nodeNumber, 1) + pi*tankGeom.R^2*layerHeightsEqual;
    end
    % Return the real component
    volumes = real(volumes);
end