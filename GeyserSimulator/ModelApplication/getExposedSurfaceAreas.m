function [A_exposed, valid] = getExposedSurfaceAreas(tankGeom, plotAreas)
    
    if(nargin < 2)
        plotAreas = false;
    end
    R = tankGeom.R;
    
    % If the tank is horizontally orientated
    if(tankGeom.orientation == 'H')
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

    % If the tank is Vertically orientated
    elseif(tankGeom.orientation == 'V')
        z_delta_vertical = tankGeomModel.L/tankGeomModel.n;
        A_exposed_vertical_long = zeros(tankGeomModel.n, 1) + 2*pi*tankGeomModel.R*z_delta_vertical;
        A_exposed_vertical_ends = zeros(tankGeomModel.n, 1);
        A_exposed_vertical_ends([1, end]) = pi*tankGeomModel.R^2;
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