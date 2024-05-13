function [A_c] = getLayerCrossSectionalAreas(tankGeom)

    % If the tank is horizontally orientated
    if(tankGeom.orientation == 'H')
        A_crossSecFunc = @(h, R, L) 2*sqrt(h.*(2*R - h)) * L;
        tankGeom.A_crossSec = A_crossSecFunc(tankGeom.h_layers, tankGeom.R, tankGeom.L);
        A_c = tankGeom.A_crossSec;

    % If the tank is vertically orientated
    elseif(tankGeom.orientation == 'V')
        A_c = pi*tankGeomModel.R^2;
    end
end