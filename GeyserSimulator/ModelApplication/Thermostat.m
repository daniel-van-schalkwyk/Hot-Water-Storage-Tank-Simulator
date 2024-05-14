function [geyserCoilState] = Thermostat(thermostatTemp, setTemp, hysteresisBand, prevState)
% This function is responsible for controlling the power delivered 
% to the element of the geyser. The power will switch between ON and OFF
% based on the temperature reading of the internal geyser water.

    deadBandBottom = setTemp - hysteresisBand/2;
    deadBandTop = setTemp + hysteresisBand/2;

    % Check if the water temperature is below the set temperature
    if(thermostatTemp < deadBandBottom )
      % Geyser element should switch on 
      geyserCoilState = 1;
      return;
    
    % Check if the water temperature is above the set temperature
    elseif(thermostatTemp >= deadBandTop)
      % Geyser element should switch off
      geyserCoilState = 0;
      return;   

    else
        if(prevState == 1)
            geyserCoilState = 1;
        else
            geyserCoilState = 0;
        end
    end
end