function weightedMeanVec = getWeightedMean(TempBus, tankVolumePerNode)
        
    V_tot = sum(tankVolumePerNode);
    weightedAvg = 0;
    for i = 1:1:length(tankVolumePerNode)
        weightedAvg = weightedAvg + TempBus(:,i)*tankVolumePerNode(i);
    end
    weightedMeanVec = weightedAvg/V_tot;
end