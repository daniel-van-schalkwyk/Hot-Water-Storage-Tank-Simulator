function [physicalLocationIndices, inletIndex, outletIndex] = getPhysicalLayerIndices(tankGeomActual, tankGeomModel)
    % This function is used to compare the actual physical sensor heights
    % with the model sensor heights and get the sensor layers in the model
    % that best compares to the actual sensor heights. This is required
    % since the number of layers in the model is typically much larger. 

    physicalLocationIndices = zeros(5, 1);
    for i = 1:1:length(tankGeomActual.h_sensors)
        diff = tankGeomModel.h_sensors - tankGeomActual.h_sensors(i);
        [~, physicalLocationIndices(i)] = min(abs(diff));
    end
    
    try
        % Check if index for the inlet should be calculated
        if(isfield(tankGeomActual, 'h_inlet'))
            diff = tankGeomModel.h_sensors - tankGeomActual.h_inlet;
            [~, inletIndex] = min(abs(diff));
        end
    catch 

    end
    
    try
        % Check if index for the outlet should be calculated
        if(isfield(tankGeomActual, 'h_outlet'))
            diff = tankGeomModel.h_sensors - tankGeomActual.h_outlet;
            [~, outletIndex] = min(abs(diff));
        end
    catch 

    end
    
end