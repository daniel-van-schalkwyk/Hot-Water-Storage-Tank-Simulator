D_tank = 0.4;
t_tank = 0.0015;
V_tank = 0.150;
TC_locations = [0.02, 0.0638, 0.1075, 0.1513, 0.195, 0.2388, 0.2825, 0.3263, 0.370] + 5e-3; %m
TC_locations_norm = TC_locations/(D_tank);
H_dimensionless = flip(TC_locations_norm);
tankGeomModel.t = 0.0015;                                   % Tank thickness [m]
tankGeomModel.L = 1.21;                                     % Length [m]
tankGeomModel.R = 0.4/2 - tankGeomModel.t;                  % Radius (minus tank thickness) [m]
tankGeomModel.n = 9;
tankGeomModel.orientation = 'H';
tankGeomModel.layerConfig = 'CH'; % CV = Constant volume, variable layer height && CH = constant height, variable volume

tankGeomMore = tankGeomModel;
tankGeomMore.n = 9*5;
% Calculate other geometrical facets
tankGeomModel = EwhGeometryTools.populateTankGeometry(tankGeomModel);
tankGeomMore = EwhGeometryTools.populateTankGeometry(tankGeomMore);

g_coeffsNorm = [8.3056
    8.0019
    8.2120
    8.3463
    8.0901
    7.4802
    5.5281
         0
         0];

% Define custom equation for S-curve (sigmoidal function)
sigmoidal_eqn = @(a, b, c, x) a ./ (1 + exp(-b * (x - c)));

sigmoidal_eqn2 = @(a,b,c,d,g,h) a + (b-a)./((1+(h./c).^d).^g);

skewed_sigmoidal_eqn3 = @(a, b, c, d, x) a ./ (1 + exp(-b * (x - c))) + d;

h = linspace(0, 1, 100);
figure
plot(h, sigmoidal_eqn2(0, 8.1, 0.44, -20, 0.4, h))

% Initial guess for parameters (a, b, c) 
initial_guess = [8, 50, 0.4];

% Fit the S-curve using the custom equation
fit_result = fit(flip(tankGeomModel.h_sensorsNorm'), g_coeffsNorm, sigmoidal_eqn);

% Generate new x values for interpolation or prediction
new_h = tankGeomMore.h_sensorsNorm;

% Interpolate/predict new y values using the fitted curve
% new_g_norm = polyval(fit_result2, new_h');
new_g_norm = sigmoidal_eqn2(0, 9, 0.35, -15, 0.4, new_h');
new_g = new_g_norm.*tankGeomMore.layerVolumes;
old_g = flip(g_coeffsNorm.*tankGeomModel.layerVolumes);

figure
plot(tankGeomModel.h_sensorsNorm, old_g, "+k", MarkerSize=10); hold on;
plot(tankGeomMore.h_sensorsNorm, new_g, "+b", MarkerSize=10); hold off;
legend(sprintf("Old g (sum = %d)", sum(old_g)), sprintf("New g (sum = %d)", sum(new_g)))

% Plot the original data and the fitted curve
figure
plot(flip(tankGeomModel.h_sensorsNorm'), g_coeffsNorm, 'ro', 'DisplayName', 'Original Data');
hold on;
plot(new_h, new_g_norm, 'b-', 'DisplayName', 'Fitted Curve');
hold off;
xlabel('X');
ylabel('Y');
legend;