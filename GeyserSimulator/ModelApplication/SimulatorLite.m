function SimulatorLite(uid, settingsPath, configPath)
    %% Geyser Simulator LITE
    % Read settings for geyser simulation
    settings = ini2struct(settingsPath); 
    
    % Define MQTT broker parameters from settings
    broker = "ssl://" + settings.ConnectionDetails.masterBrokerUrl;  
    port = int32(settings.ConnectionDetails.port);
    username = settings.ConnectionDetails.username;
    password = settings.ConnectionDetails.password;
    certificateRoot = settings.ConnectionDetails.certPath;
    qos = settings.ConnectionDetails.qos;

    % Get pub and sub topics
    global updateTopic setupTopic stateTopic pubDataTopic pubInfoTopic 
    updateTopic = append(uid, settings.SimulatorSubTopics.geyserUpdateTopicSubRoot);
    setupTopic = append(uid, settings.SimulatorSubTopics.geyserSetupTopicSubRoot);
    stateTopic = append(uid, settings.SimulatorSubTopics.geyserStateTopicSubRoot);
    pubDataTopic = append(uid, settings.SimulatorPubTopics.geyserDataTopicSubRoot);
    pubInfoTopic = append(uid, settings.SimulatorPubTopics.geyserInfoTopicSubRoot);
    
    % Initialise global volatile variables
    global updateData stateData setupData 
    global updateReceived stateReceived setupReceived 
    global simParams inputs simTimeStamp
    global client updateTimer

    % Connect to MQTT master client
    try
        client = ConnectMqtt(broker, port, uid, username, password, certificateRoot);
    catch err
        throw err;
    end
    
    % Subscribe to topics
    subscribe(client, updateTopic, Callback=@updateCallBackFunc, QualityOfService=qos)
    subscribe(client, setupTopic, Callback=@updateCallBackFunc, QualityOfService=qos)
    subscribe(client, stateTopic, Callback=@updateCallBackFunc, QualityOfService=qos)
    
    % Import config data 
    try 
        configData = importConfigData(configPath);
    catch err
        publishError(err, client);
        return;
    end
    
    % Generate the Geometric data for the tank
    try 
        tankGeom = generateTankCharacteristics(configData);
    catch err
        publishError(err, client);
        return;
    end

    % Get all model parameters
    try 
        modelParams = importModelParameters(configData);
    catch err
        publishError(err, client);
        return;
    end
    
    % Loop indefinitely to keep the listener running
    ListenToMqtt(client, tankGeom, modelParams, uid)

    %% Main broker listener loop
    function [] = ListenToMqtt(client, tankGeomData, modelParameters, uid)

        simParams.gCoeffs = modelParameters.g_coeffs;
            simParams.h_ThermostatNorm = tankGeomData.h_thermistor_rel;
            simParams.hysteresisBand = tankGeomData.hysteresisBand;
            simParams.layerMixPortions = zeros(tankGeomData.n-1, 1);
            simParams.layerMixPortions([1:1, end-1:end]) = 0;
            simParams.U_amb = modelParameters.U_Ambient;
            simParams.U_layers = modelParameters.U_layers;
            simParams.n_mix_charge = modelParameters.n_mix_charge;
            simParams.n_mix_discharge = modelParameters.n_mix_discharge;
            simParams.rho_w = @(T) (1.49343e-3 - 3.7164e-6*T + 7.09782e-9*T.^2 - 1.90321e-20*T.^6).^-1;                    
            simParams.cp_w = @(T) 8.15599e3 - 2.80627*10*T + 5.11283e-2*T.^2 - 2.17582e-13*T.^6; 
            simParams.delta_t_s = modelParameters.dt;

        % Loop indefinitely to keep the listener running
        while true
            if(updateReceived)
                % Acknowledge message
                publishAck("Update received", client)
                updateReceived = false;
                % Interpret the update message
                UpdateGeyserStates(updateData, modelParameters);
                try
                    detailed = logical(updateData.Detailed);
                catch
                    detailed = false;
                end
                % Only run model if One shot is requested under update
                % message
                if(strcmp(updateData.Mode, "OneShot"))
                    publishAck("OneShot requested", client)
                    stopAllTimers();
                    try
                        % Run model with provided inputs
                        Results = GeyserModel(tankGeomData, modelParameters, detailed, uid);
                        % Publish results
                        publishMessage(client, pubDataTopic, jsonencode(Results))
                    catch err
                        publishError(err, client);
                    end
                end
                
            
            elseif(stateReceived)
                % Acknowledge message
                publishAck("Sim state received", client)
                stateReceived = false;

                % Interpret the state message
                try
                   if(strcmp(stateData.Mode, "Stop"))
                       publishAck("Stop state received", client)
                       stop(updateTimer)
                       publishAck("Continuous mode inactive", client)
                   elseif(strcmp(stateData.Mode, "Start"))
                       publishAck("Start state received", client)
                       start(updateTimer);
                       publishAck("Continuous mode active", client)
                   elseif(strcmp(stateData.Mode, "Exit"))
                       publishAck("Exit state received. Client will disconnect.", client)
                       stopAllTimers();
                       break;
                   else
                       ex = MException("Unrecognised Value", "'Mode' value unrecognised: Either 'Start', 'Stop', or 'Exit'");
                       ex.throw;
                   end
                catch err
                    publishError(err, client);
                end
            elseif(setupReceived)
                % Acknowledge message
                publishAck("Sim setup received", client)
                setupReceived = false;
                try
                    detailed = logical(setupData.Detailed);
                catch
                    detailed = false;
                end
                
                % Interpret the setup message
                try
                    if(strcmp(setupData.Mode, "Continuous"))
                        % Extract setup info and populate into update
                        % parameters
                        UpdateGeyserStates(setupData, modelParameters);

                        % Delete existing timers
                        stopAllTimers

                        % Create timer with geyser model handler
                        try 
                            simTimeStamp = datetime(setupData.Params.SimDateTime,'InputFormat','uuuu-MM-dd''T''HH:mm:ss','TimeZone', 'local');
                        catch
                            simTimeStamp = datetime('now', 'Format','uuuu-MM-dd''T''HH:mm:ss', 'TimeZone','local');
                        end
                        
                        % Setup the callback timer
                        updateTimer = timer(Period=setupData.Params.Duration_s, ExecutionMode="fixedRate", BusyMode="drop", TasksToExecute=inf, StartDelay=0, TimerFcn={@GeyserModelHandler, tankGeomData, modelParameters, detailed, uid});
                        start(updateTimer);
                    elseif(strcmp(setupData.Mode, "OneShot"))
                        % Delete existing timers
                        simTimeStamp = datetime(setupData.Params.SimDateTime,'InputFormat','uuuu-MM-dd''T''HH:mm:ss','TimeZone','local');
                        stopAllTimers
                    else
                        ex = MException("Unrecognised Value", "'Mode' value unrecognised: Either 'Continuous' or 'OneShot'");
                        ex.throw;
                    end
                catch err
                    publishError(err, client);
                end
            end

            % Wait 5ms
            pause(0.005); 
        end
    end
    
    %% A handler function for the update timer 
    function GeyserModelHandler(obj, event, tankGeomData, modelParameters, detailed, uid)
        
        % Call the geyser model with current parameters
        try
            % Run model with provided inputs
            Results = GeyserModel(tankGeomData, modelParameters, detailed, uid);
            % Publish results
            publishMessage(client, pubDataTopic, jsonencode(Results))
        catch err
            publishError(err, client);
        end

    end

    %% A generic method that is used to update the geyser states during continuous mode
    function UpdateGeyserStates(geyserStateData, modelParams)
        % Attempt to update data
        try 
            if(strcmp(geyserStateData.Type, "Setup"))
                simParams.simTime_steps = (geyserStateData.Params.TimeScale * geyserStateData.Params.Duration_s) / modelParams.dt;
                simParams.T_initial = geyserStateData.Params.States.T_Profile;   

                % Extract params from Setup message
                infoMessage = "";
                try 
                    simParams.eHeatingPower = geyserStateData.Params.States.Power;
                    inputs.Power = geyserStateData.Params.States.Power;
                    infoMessage = infoMessage + "Power setup to " + string(simParams.eHeatingPower);
                catch
                    simParams.eHeatingPower = 3000;
                    inputs.Power = 3000;
                end
                try 
                    simParams.setTemp = geyserStateData.Params.States.SetTemp;
                    infoMessage = infoMessage + "; Set temp setup to " + string(simParams.setTemp);
                catch
                    simParams.setTemp = 60;
                end
                try 
                    inputs.T_inlet = geyserStateData.Params.States.InletTemp;
                    infoMessage = infoMessage + "; Inlet temp setup to " + string(inputs.T_inlet);
                catch
                    inputs.T_inlet = 15;
                end
                try 
                    inputs.T_amb = geyserStateData.Params.States.AmbientTemp;
                    infoMessage = infoMessage + "; Ambient temp setup to " + string(inputs.T_amb);
                catch
                    inputs.T_amb = 20;
                end
                try 
                    inputs.flowrate = geyserStateData.Params.States.FlowRate;
                    infoMessage = infoMessage + "; Flow rate setup to " + string(inputs.flowrate);
                catch
                    inputs.flowrate = 0;
                end
                publishAck(infoMessage, client)
            elseif(strcmp(geyserStateData.Type, "Update"))   
                infoMessage = "";
                try 
                    simParams.eHeatingPower = geyserStateData.Params.States.Power;
                    inputs.Power = geyserStateData.Params.States.Power;
                    infoMessage = infoMessage + "Power updated to " + string(simParams.eHeatingPower);
                catch
                end
                try 
                    simParams.setTemp = geyserStateData.Params.States.SetTemp;
                    infoMessage = infoMessage + "; Set temp updated to " + string(simParams.setTemp);
                catch
                end
                try 
                    inputs.T_inlet = geyserStateData.Params.States.InletTemp;
                    infoMessage = infoMessage + "; Inlet temp updated to " + string(inputs.T_inlet);
                catch
                end
                try 
                    inputs.T_amb = geyserStateData.Params.States.AmbientTemp;
                    infoMessage = infoMessage + "; Ambient temp updated to " + string(inputs.T_amb);
                catch
                end
                try 
                    inputs.flowrate = geyserStateData.Params.States.FlowRate;
                    infoMessage = infoMessage + "; Flow rate updated to " + string(inputs.flowrate);
                catch
                end
                publishAck(infoMessage, client)
            end   
        catch err
            publishError(err, client);
            return;
        end
        
    end

    %% The entry point for the geyser model
    function Results = GeyserModel(tankGeomData, modelParams, detailed, uid)
        % Call the main generic state-space function with prepared
        % inputs

        if(nargin < 3)
            detailed = false;
        end
        try 
            % run the model
            [T_mat_sim, ~, coilStates, thermostatTemps] = StateSpaceConvectionMixingModel(tankGeomData, simParams, inputs);

            % Update Parameters and send results
            simTimeStamp = simTimeStamp + seconds(simParams.simTime_steps * modelParams.dt);
            Results.Timestamp_sim = datetime(simTimeStamp, "Format", "uuuu-MM-dd'T'HH:mm:ss.SSS");
            Results.Uid = uid;
            Results.Type = "Data";
            
            if(detailed)
                
                Results.States.SetTemp = simParams.setTemp;
                Results.States.CoilActive = logical(coilStates);
                Results.States.ThermostatTemp = thermostatTemps;
                Results.Inputs = inputs;
                Results.T_mean = getWeightedMean(T_mat_sim, tankGeomData.layerVolumes);
                [Ex_tank, ~, ~, ~, ~, U_tank] = GetExergyNumber(T_mat_sim, tankGeomData.layerVolumes', tankGeomData.V, modelParams.T_ref + 273.15, simParams.rho_w, simParams.cp_w); 
                [~, ~, ~, ~, ~, U_tank_full] = GetExergyNumber(zeros(1, tankGeomData.n)+simParams.setTemp+tankGeomData.hysteresisBand/2, tankGeomData.layerVolumes', tankGeomData.V, modelParams.T_ref + 273.15, simParams.rho_w, simParams.cp_w); 
                Results.Energy = U_tank/3600/1000;
                Results.Exergy = Ex_tank/3600/1000;
                Results.SOC = U_tank./U_tank_full*100;
                Results.T_Profile = T_mat_sim;
                Results.SOC = U_tank./U_tank_full*100;
            else
                Results.States.SetTemp = simParams.setTemp;
                Results.States.CoilActive = logical(coilStates(end));
                Results.States.ThermostatTemp = thermostatTemps(end);
                Results.Inputs = inputs;
                Results.T_mean = getWeightedMean(T_mat_sim(end, :), tankGeomData.layerVolumes);
                [Ex_tank, ~, ~, ~, ~, U_tank] = GetExergyNumber(T_mat_sim(end, :), tankGeomData.layerVolumes', tankGeomData.V, modelParams.T_ref + 273.15, simParams.rho_w, simParams.cp_w); 
                [~, ~, ~, ~, ~, U_tank_full] = GetExergyNumber(zeros(1, tankGeomData.n)+simParams.setTemp+tankGeomData.hysteresisBand/2, tankGeomData.layerVolumes', tankGeomData.V, modelParams.T_ref + 273.15, simParams.rho_w, simParams.cp_w); 
                Results.Energy = U_tank/3600/1000;
                Results.Exergy = Ex_tank/3600/1000;
                Results.SOC = U_tank/U_tank_full*100;
                Results.T_Profile = T_mat_sim(end, :);
                Results.SOC = U_tank/U_tank_full*100;
            end

            % Update T_profile for next iteration
            simParams.T_initial = T_mat_sim(end, :)';

        catch err
            publishError(err, client);
            return;
        end
    end

    %% Client connection method used to establish a connection with the master broker
    function client = ConnectMqtt(brokerUrl, port, uid, username, password, certificateRoot)
        % Method used to connect to the broker using connection information
        % from the settings file.
        client = mqttclient(brokerUrl, Port = port, ClientID = uid, Username = username, Password = password, CARootCertificate = certificateRoot);
    
    end
    
    %% Callback unctio that needs to be triggered when message is received
    function updateCallBackFunc(topic, data)
        % Callback method to intercept MQTT messages and to poulate into
        % volatile variables
        disp(topic);
        disp(data);
        % Trigger updateFlag
        if(topic == updateTopic)
            try 
                updateData = jsondecode(data);
                updateReceived = true;
            catch err
                publishError(err, client);
            end
        % Trigger state flag
        elseif(topic == stateTopic)
            try 
                stateData = jsondecode(data);
                stateReceived = true;
            catch err
                publishError(err, client);
            end
        % Trigger setup flag
        elseif(topic == setupTopic)
            try 
                setupData = jsondecode(data);
                setupReceived = true;
            catch err
                publishError(err, client);
            end
        end     
    end
    
    %% Used to write messages to the master broker
    function publishMessage(client, topic, payload)
        write(client, topic, payload, QualityOfService = 1, Retain = false)
    end

    %% Used to publish errors encountered in the code to the broker
    function publishError(err, client)
        errMessage.Type = "Error";
        errMessage.Timestamp = datetime("now", "Format", "uuuu-MM-dd'T'HH:mm:ss.SSS");
        errMessage.Cause = err.cause;
        errMessage.Description = err.message;
        errMessage.StackTrace = err.stack.name;
        errMessage.Line = err.stack.line;
        publishMessage(client, pubInfoTopic, jsonencode(errMessage))
    end

    %% Used to publish Acknowledge messages to the broker
    function publishAck(message, client)
        ackMessage.Type = "ACK";
        ackMessage.Timestamp = datetime("now", "Format", "uuuu-MM-dd'T'HH:mm:ss.SSS");
        ackMessage.Description = message;
        publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
    end
    
    %% Get the configuration data needed for simulation
    function configData = importConfigData(configJsonFileUrl)
        % Open and read the configuration file
        try
            fid = fopen(configJsonFileUrl, 'r');
            str = fread(fid, '*char')';
            fclose(fid);
        catch ex
            fprintf('An error occurred: %s\n', ex.message);
            throw exception;
        end

        % Decode the configuration file
        try 
            configData = jsondecode(str);
        catch ex
            fprintf('An error occurred: %s\n', ex.message);
            throw exception;
        end
    end
    
    %% Generate tank geometry object
    function geyserOut = generateTankCharacteristics(configJson)
        try 
            % Get geometric attributes
            geyserOut.t = configJson.geyser.tWall;                               
            geyserOut.L = configJson.geyser.longLength;                          
            geyserOut.R = configJson.geyser.diameter/2 - geyserOut.t;
            geyserOut.h_thermistor_rel = configJson.geyser.h_thermistor_rel;
            geyserOut.orientation = configJson.geyser.orientation;

            % Get model stuff relatng to geomerty
            geyserOut.layerConfig = configJson.modelParameters.layerConfig;
            geyserOut.n = configJson.modelParameters.nodeNumber;
                       
            % Populate the more advanced geometric characteristics
            geyserOut = EwhGeometryTools.populateTankGeometry(geyserOut);

            % Get other characteristics
            geyserOut.coilPower = configJson.geyser.coilPower;
            geyserOut.setTemp = configJson.geyser.setTemp;
            geyserOut.hysteresisBand = configJson.geyser.hysteresisBand;

        catch ex
            fprintf('An error occurred: %s\n', ex.message);
            throw ex;
        end
    end

    %% Finds any timer and stops it
    function stopAllTimers()
        T = timerfind;
        if ~isempty(T)
            stop(T)
            delete(T)
        end
    end
    %% Function to import model parameters
    function modelParams = importModelParameters(configData)
        try 
            modelParams.n_mix_discharge = configData.modelParameters.n_mix_discharge; 
            modelParams.n_mix_charge = configData.modelParameters.n_mix_charge; 
            modelParams.dt = configData.modelParameters.dt; 
            modelParams.tempInit = configData.modelParameters.tempInit;
            modelParams.U_layers = configData.modelParameters.U_layers;
            modelParams.U_Ambient = configData.modelParameters.U_Ambient;
            modelParams.g_coeffs = configData.modelParameters.g_coeffs;  
            modelParams.T_ref = configData.modelParameters.T_ref;
        catch error
            fprintf('An error occurred: %s\n', error.message);
            throw ex;
        end  
    end
end



