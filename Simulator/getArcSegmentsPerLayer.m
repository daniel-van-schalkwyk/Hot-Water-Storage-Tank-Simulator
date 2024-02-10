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
    valid = abs(p_circ - p_all_layers) < 1e-9;
end