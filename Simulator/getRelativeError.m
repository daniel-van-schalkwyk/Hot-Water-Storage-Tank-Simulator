function [T_relativeError, T_MRE, T_MRE_all, A] = getRelativeError(actual, simulated)
    % Actual and predicted need to be column vectors!
    tempDiff = actual - simulated;
    absTempDiff = abs(tempDiff);
    T_relativeError = absTempDiff./abs(actual);
    T_MRE = mean(T_relativeError, 1);
    T_MRE_all = mean(T_MRE);
    A = (1 - T_MRE_all)*100;
end