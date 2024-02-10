function [Ex_actual, Ex_ideal, Ex_mixed, Ex_number, Ex_exp_perLayer, U_tank, U_tank_avg] = GetExergyNumber(strVector, nodeVolumes, V_tank, T_ref, rho_w, cp_w, smoothValue)
    
    if(nargin < 7)
        smoothValue = 100;
    end
    cp_all = cp_w(strVector + 273.15);
    rho_all = rho_w(strVector+ 273.15);
    U_tank = ( rho_all.*cp_all.*(strVector+273.15 - T_ref)) * nodeVolumes'; 

    T_m = getWeightedMean(strVector, flip(nodeVolumes));
    U_tank_avg = rho_w(T_m + 273.15).*cp_w(T_m + 273.15).*sum(nodeVolumes).*(T_m + 273.15 - T_ref);

    Ex_exp_perLayer = (rho_w(strVector+273.15).*cp_w(strVector+273.15).* ...
        ((strVector+273.15 - T_ref) - T_ref.*log((strVector+273.15)/T_ref))) .* flip(nodeVolumes);
    Ex_actual = filloutliers((rho_w(strVector+273.15).*cp_w(strVector+273.15).* ...
        ((strVector+273.15 - T_ref) - T_ref.*log((strVector+273.15)/T_ref))) * flip(nodeVolumes'), "makima", "movmean", 30);
    Ex_actual = smooth(Ex_actual, "rloess", smoothValue);
    
    Ex_mixed = (rho_w(T_m+273.15).*cp_w(T_m+273.15).* ...
        ((T_m+273.15 - T_ref) - T_ref.*log((T_m+273.15)/T_ref))) .* V_tank;
    Ex_mixed = smooth(Ex_mixed, "rloess", smoothValue);
    
    V_upper = @(Q_exp, T_u, T_b) ( Q_exp - rho_w(T_b).*cp_w(T_b).*V_tank.*(T_b - T_ref) ) ./ ( rho_w(T_u).*cp_w(T_u).*(T_u - T_ref) - rho_w(T_b).*cp_w(T_b).*(T_b - T_ref) );
    V_u = V_upper(U_tank, strVector(:, 1)+273.15, strVector(:, end) + 273.15);
    V_b = V_tank - V_u;

    % Ideal exergy
    Ex_ideal = (rho_w(strVector(:, 1)+273.15).*cp_w(strVector(:, 1)+273.15).*V_u...
        .*(strVector(:, 1)+273.15 - T_ref - T_ref.*log((strVector(:, 1)+273.15)/T_ref))) ...
        + (rho_w(strVector(:, end)+273.15).*cp_w(strVector(:, end)+273.15).*V_b...
        .*(strVector(:, end)+273.15 - T_ref - T_ref.*log((strVector(:, end)+273.15)/T_ref)));
    Ex_ideal = smooth(Ex_ideal, "rloess", smoothValue);

    % Return the exergy number
    Ex_number = movmean(filloutliers((Ex_actual - Ex_mixed)./(Ex_ideal - Ex_mixed), "makima", "movmean", 50), 10);
end