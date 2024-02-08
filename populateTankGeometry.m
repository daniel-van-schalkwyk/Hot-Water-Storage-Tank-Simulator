function tankGeometryStruct = populateTankGeometry(tankGeom)
    tankGeom.V = pi*tankGeom.R^2*tankGeom.L;     % Volume [m^3]
    % Calculated base on tankGeom input
    if(strcmp(tankGeom.orientation, 'vertical'))
        tankGeom.z_delta = tankGeom.L/tankGeom.n;
        tankGeom.h_layers = linspace(tankGeom.z_delta, tankGeom.L, tankGeom.n);
        tankGeom.h_sensors = tankGeom.h_layers - tankGeom.z_delta/2;
        % Determine the node volumes of the CHP buffer tank
        tankGeom.layerVolumes = getNodeVolumes(tankGeom);
    elseif(strcmp(tankGeom.orientation, 'horizontal'))
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
    
    % Return fully populated geometrical model
    tankGeometryStruct = tankGeom;
end