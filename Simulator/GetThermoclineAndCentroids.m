function [yLengths, Vhot, A_therm] = GetThermoclineAndCentroids(tankGeom, V_cold)

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
    h = GetRootWithBisectionMethod(V_cold_func, 0, 2*R, 1e-9);

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

%% Bisection Method
function [root] = GetRootWithBisectionMethod(func, min, max, tol)
    % Define search window
    rootMin = min;
    rootMax = max;

    % Initiate loop
    counter = 0;
    result_est = mean([min, max]);
    while(abs(result_est) > tol)
        if(counter >= 100)                   % Check break condition
%             fprintf("Max iteration count reached: root = %.6f\n", root_est)   % Report estimate
            break;
        end
        
        root_est = mean([rootMin, rootMax]);   % Calculate new midsection
        result_est = func(root_est);        % Calculate result based on new midsection
        if(result_est == 0 || abs(rootMax-rootMin) < tol)
            % Result found
%             fprintf("Root found = %.6f\n", root_est)   % Report estimate
            break
        end

        if(sign(result_est) == -1)
            rootMin = root_est;             % Max search window changes
        elseif(sign(result_est) == 1)
            rootMax = root_est; 
        end
        counter = counter + 1;
%     fprintf("Est root = %.6f\n", root_est)   % Report estimate
    end
    root = root_est;                        % Assign new estimated root
end