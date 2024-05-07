#ifndef SIMULATOR_MODEL_H
/**
 * \brief 
 */
#define SIMULATOR_MODEL_H

#include <vector>
#include <string>

class ConvectionModel
{
    public:
    /**
     * \brief
     * The main model that is called to calculate the temperature profiles inside of the tank
     * \param tank_geom_model dd
     * \param simParams ss
     * \param inputs ss
     * \return ss
     */
    static std::vector<std::vector<double>> StateSpaceConvectionModel(const std::vector<double>& tank_geom_model,
                                                                      const std::vector<double>& simParams,
                                                                      const std::vector<double>& inputs);
    private:
    static std::pair<std::vector<double>, bool> getExposedSurfaceAreas(const std::vector<double>& tankGeomModel);
    static std::vector<double> getLayerCrossSectionalAreas(const std::vector<double>& tankGeomModel);
    static std::vector<double> getNodeVolumes(const std::vector<double>& tankGeomModel,
                                              std::string orientation,
                                              bool option);
    static double rho_w(double T);
    static double cp_w(double T);
};

#endif 
