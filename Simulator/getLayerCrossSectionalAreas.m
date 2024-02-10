function [A_c] = getLayerCrossSectionalAreas(tankGeom)
    A_crossSecFunc = @(h, R, L) 2*sqrt(h.*(2*R - h)) * L;
    tankGeom.A_crossSec = A_crossSecFunc(tankGeom.h_layers, tankGeom.R, tankGeom.L);
    A_c = tankGeom.A_crossSec;
end