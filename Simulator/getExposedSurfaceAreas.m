function [A_exposed, valid] = getExposedSurfaceAreas(tankGeom, plotAreas)
    
    if(nargin < 2)
        plotAreas = false;
    end
    R = tankGeom.R;
    [arcLengthPerLayer, valid] = getArcSegmentsPerLayer(tankGeom.h_layers, R);
    if(~valid)
        error('Circular perimeter does not equal sum of layer arc lengths')
    end

    % Calculate the surface areas
    layerVolumes = getNodeVolumes(tankGeom);
    layerCircAreas = layerVolumes./tankGeom.L;
    valid = abs(pi*tankGeom.R^2 - sum(layerCircAreas)) < 1e-8;
    if(~valid)
        error('BC not met: Full circular area does not equal sum of layer areas on circular face')
    end
    
    % Calculate full surface area of layer exposed to environment
    A_exposed = 2*layerCircAreas + arcLengthPerLayer'*tankGeom.L;
    
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