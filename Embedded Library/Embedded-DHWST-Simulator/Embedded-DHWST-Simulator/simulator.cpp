#include "simulator.h"
#include <algorithm>
#include <iostream>

std::pair<std::vector<double>, bool> ConvectionModel::getExposedSurfaceAreas(const std::vector<double>& tankGeomModel) {
    // Implementation
    return std::make_pair(std::vector<double>(), false); // Placeholder implementation
}

std::vector<double> ConvectionModel::getLayerCrossSectionalAreas(const std::vector<double>& tankGeomModel) {
    // Implementation
    return std::vector<double>(); // Placeholder implementation
}

std::vector<double> ConvectionModel::getNodeVolumes(const std::vector<double>& tankGeomModel,
                                                    std::string orientation,
                                                    bool option) {
    // Implementation
    return std::vector<double>(); // Placeholder implementation
}

double ConvectionModel::rho_w(double T) {
    // Implementation
    return 0; // Placeholder implementation
}

double ConvectionModel::cp_w(double T) {
    // Implementation
    return 0; // Placeholder implementation
}

std::vector<std::vector<double>> ConvectionModel::StateSpaceConvectionModel(const std::vector<double>& tankGeomModel,
                                                                            const std::vector<double>& simParams,
                                                                            const std::vector<double>& inputs)
{
    // Extract simulation parameters and inputs
    int simTime_steps = simParams[0];
    double delta_t_s = simParams[1];
    double T_initial = simParams[2];
    double U_amb = simParams[3];
    double U_layers = simParams[4];
    int n_mix_charge = simParams[5];
    int n_mix_discharge = simParams[6];

    // Extract input data
    double T_inlet = inputs[0];
    double T_amb = inputs[1];
    double m_flowrate = inputs[2];

    // Assign number of nodes of model
    int nodes = tankGeomModel[3];

    // Calculate the exposed surface area to the ambient
    std::pair<std::vector<double>, bool> exposedSurfaceAreas = getExposedSurfaceAreas(tankGeomModel);
    std::vector<double> A_exposed = exposedSurfaceAreas.first;
    bool valid = exposedSurfaceAreas.second;
    if (!valid) {
        // Handle validation error
        std::cerr << "Validation for exposed surface area not satisfied" << std::endl;
    }

    // Calculate the cross-sectional areas of the layers
    std::vector<double> A_crossSec = getLayerCrossSectionalAreas(tankGeomModel);

    // Get layer volumes
    std::vector<double> layerVolumes = getNodeVolumes(tankGeomModel, "horizontal", true);

    // Simulation run
    std::vector<std::vector<double>> T_mat(simTime_steps, std::vector<double>(nodes));
    std::vector<std::vector<double>> dTdt_mat(simTime_steps, std::vector<double>(nodes));
    std::vector<double> T_vec_next(nodes, 0.0);
    std::vector<double> T_vec_current(nodes, T_initial);
    for (int time = 0; time < simTime_steps; time++) {
        // Get the current mass flow rate
        double massFlow_current = m_flowrate;

        // Get current ambient temp T_amb
        double T_amb_current = T_amb;

        // Check the state of flow: charging or discharging
        std::string flowState;
        double T_inlet_current;
        int n_mix;
        if (massFlow_current >= 0) {
            flowState = "discharge";
            T_inlet_current = T_inlet;
            n_mix = n_mix_discharge;
        } else {
            flowState = "charge";
            massFlow_current *= -1;
            reverse(T_vec_current.begin(), T_vec_current.end());
            T_inlet_current = T_inlet;
            n_mix = n_mix_charge;
        }

        // Calculate the accurate layer masses and capacities for the current time iteration
        std::vector<double> layerMasses(nodes);
        std::vector<double> layerCapacities(nodes);
        for (int layer = 0; layer < nodes; layer++) {
            layerMasses[layer] = rho_w(T_vec_current[layer] + 273.15) * layerVolumes[layer];
            layerCapacities[layer] = layerMasses[layer] * cp_w(T_vec_current[layer] + 273.15);
        }

        // Construct Matrix F
        std::vector<std::vector<double>> F_mat(nodes, std::vector<double>(nodes, 0.0));
        for (int layer = 0; layer < nodes; layer++) {
            auto hx_prevLayer = [&](int layer) { return U_layers * A_crossSec[layer-1] / layerCapacities[layer]; };
            auto hx_nextLayer = [&](int layer) { return U_layers * A_crossSec[layer] / layerCapacities[layer]; };
            auto hx_ambient = [&](int layer) { return U_amb * A_exposed[layer] / layerCapacities[layer]; };
            if (layer == 0) {
                F_mat[layer][layer+1] = hx_nextLayer(layer);
                F_mat[layer][layer] = -(hx_ambient(layer) + hx_nextLayer(layer));
            } else if (layer == nodes - 1) {
                F_mat[layer][layer-1] = hx_prevLayer(layer);
                F_mat[layer][layer] = -(hx_ambient(layer) + hx_prevLayer(layer));
            } else {
                F_mat[layer][layer-1] = hx_prevLayer(layer);
                F_mat[layer][layer+1] = hx_nextLayer(layer);
                F_mat[layer][layer] = -(hx_ambient(layer) + hx_nextLayer(layer) + hx_prevLayer(layer));
            }
        }

        // Construct input vector
        std::vector<double> u_input = {massFlow_current, massFlow_current * T_inlet_current, T_amb_current};

        // Construct the G matrix
        std::vector<std::vector<double>> G_mat(nodes, std::vector<double>(3, 0.0));
        for (int layer = 0; layer < nodes; layer++) {
            if (layer < n_mix) {
                if (layer == 0) {
                    G_mat[layer][0] = layer / (n_mix * layerMasses[layer]) * -T_vec_current[layer];
                } else {
                    G_mat[layer][0] = 1 / (n_mix * layerMasses[layer]) * ((layer - 1) * T_vec_current[layer-1] - layer * T_vec_current[layer]);
                }
            } else {
                G_mat[layer][0] = 1 / layerMasses[layer] * (T_vec_current[layer-1] - T_vec_current[layer]);
            }
            G_mat[layer][1] = 1 / (n_mix * layerMasses[layer]);
            G_mat[layer][2] = U_amb * A_exposed[layer] / layerCapacities[layer];
        }

        // Calculate the change of temperature over time
        std::vector<double> dTdt(nodes, 0.0);
        for (int i = 0; i < nodes; i++) {
            for (int j = 0; j < nodes; j++) {
                dTdt[i] += F_mat[i][j] * T_vec_current[j];
            }
            for (int j = 0; j < 3; j++) {
                dTdt[i] += G_mat[i][j] * u_input[j];
            }
            dTdt_mat[time][i] = dTdt[i];
        }

        // Calculate the next temperature vectors
        if (flowState == "charge") {
            std::reverse(T_vec_current.begin(), T_vec_current.end());
        }
        for (int i = 0; i < nodes; i++) {
            T_vec_next[i] = T_vec_current[i] + delta_t_s * dTdt[i];
        }
        T_mat[time] = T_vec_next;

        // Update current temperature vector
        T_vec_current = T_vec_next;
    }
    return T_mat;
}
