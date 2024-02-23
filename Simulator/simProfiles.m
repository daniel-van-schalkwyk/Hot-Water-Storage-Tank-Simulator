classdef simProfiles
    %UNTITLED Summary of this class goes here
    %   Detailed explanation goes here

    properties
        time 
        coilPower 
        ambientTemp 
        setTemp 
        flowRate 
        inletTemp
        powerAvailable 
    end

    methods
        % Constructor method
        function obj = simProfiles(structInput)
            if nargin > 0 % Check if input argument is provided
                % Check if input is a struct
                if isstruct(structInput)
                    % Convert struct to object
                    fn = fieldnames(structInput);
                    for i = 1:length(fn)
                        if isprop(obj, fn{i})
                            obj.(fn{i}) = structInput.(fn{i});
                        end
                    end
                else
                    error('Input must be a struct.');
                end
            end
        end
    end

end