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

    % Get pub and sub topics
    global updateTopic pubDataTopic pubInfoTopic
    updateTopic = settings.SimulatorSubTopics.geyserUpdateTopicRoot + "/" + uid;
    pubDataTopic = settings.SimulatorPubTopics.geyserDataTopicRoot + "/" + uid;
    pubInfoTopic = settings.SimulatorPubTopics.geyserInfoTopicRoot + "/" + uid;
    
    % Initialise global volatile variables
    global updateData updateReceived

    % Connect to MQTT master client
    try 
        client = ConnectMqtt(broker, port, uid, username, password, certificateRoot);
    catch err
        throw err;
    end
    
    % Subscribe to topics
    subscribe(client, updateTopic, Callback=@updateCallBackFunc)
    sprintf('Subscribed to %s', updateTopic)
    
    % Import config data 
    try 
        configData = importConfigData(configPath);
    catch err
        ackMessage.Description = "Error: " + err.message;
        publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
        return;
    end
    
    % Generate the Geometric data for the tank
    try 
        tankGeom = generateTankCharacteristics(configData);
    catch err
        ackMessage.Description = "Error: " + err.message;
        publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
        return;
    end

    % Get all model parameters
    try 
        modelParams = importModelParameters(configData);
    catch err
        ackMessage.Description = "Error: " + err.message;
        publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
        return;
    end
    
    % Loop indefinitely to keep the listener running
    ListenToMqtt(client, tankGeom, modelParams)
    
    % When done, disconnect from the broker
    disconnect(client)

    %% Functions
    function [] = ListenToMqtt(client, tankGeomData, modelParameters)
        % Loop indefinitely to keep the listener running
        while true
            if(updateReceived)
                % Acknowledge message
                ackMessage.Type = "ACK";
                publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
                updateReceived = false;
                % Interpret the update message
                try
                    % Run model with provided inputs
                    Results = GeyserModel(updateData, tankGeomData, modelParameters);
                    % Publish results
                    publishMessage(client, pubDataTopic, jsonencode(Results))
                catch err
                    ErrMessage.Description = "Error: " + err.message;
                    ErrMessage.StackTrace = err.stack.name;
                    ErrMessage.Line = err.stack.line;
                    publishMessage(client, pubInfoTopic, jsonencode(ErrMessage))
                end
            end
            % Wait 5ms
            pause(0.005); 
        end
    end
    
    %% The entry point for the geyser model
    function Results = GeyserModel(geyserStateData, tankGeomData, modelParams)
        % The model that is executed and that is used to generate the
        % results
        % Prepare arguments for model
        inputs.T_inlet = geyserStateData.GeyserStates_current.InletTemp;
        inputs.T_amb = geyserStateData.GeyserStates_current.AmbientTemp;
        inputs.flowrate = geyserStateData.GeyserStates_current.FlowRate;
        nodes = tankGeomData.n;
        
        simStartDateTime = geyserStateData.Sim.SimDateTime;
        timeScale = geyserStateData.Sim.TimeScale;
        actualDuration = geyserStateData.Sim.Duration_s;
        simParams.delta_t_s = modelParams.dt;          
        simParams.simTime_steps = timeScale*actualDuration/modelParams.dt;
        simParams.rho_w = @(T) (1.49343e-3 - 3.7164e-6*T + 7.09782e-9*T.^2 - 1.90321e-20*T.^6).^-1;                    
        simParams.cp_w = @(T) 8.15599e3 - 2.80627*10*T + 5.11283e-2*T.^2 - 2.17582e-13*T.^6;      

        simParams.T_initial = geyserStateData.GeyserStates_prev.T_Profile;            
        simParams.U_amb = modelParams.U_Ambient;
        simParams.U_layers = modelParams.U_layers;
        simParams.n_mix_charge = modelParams.n_mix_charge;
        simParams.n_mix_discharge = modelParams.n_mix_discharge;
        simParams.layerMixPortions = zeros(nodes-1, 1);
        simParams.layerMixPortions([1:1, end-1:end]) = 0;
        simParams.eHeatingPower = geyserStateData.GeyserStates_current.Power;
        simParams.gCoeffs = modelParams.g_coeffs;
        simParams.h_ThermostatNorm = tankGeomData.h_thermistor_rel;
        simParams.hysteresisBand = tankGeomData.hysteresisBand;
        simParams.setTemp = tankGeomData.setTemp;

        % Call the main generic state-space function with prepared
        % inputs
        try 
            [T_mat_sim, ~, coilStates, thermostatTemps] = StateSpaceConvectionMixingModel(tankGeomData, simParams, inputs);
            Results.T_mean = getWeightedMean(T_mat_sim(end, :), tankGeomData.layerVolumes);
            Results.CoilState = coilStates(end);
            Results.ThermostatTemp = thermostatTemps(end);
            Results.T_Profile = T_mat_sim(end, :);
            [~, ~, ~, ~, ~, U_tank] = GetExergyNumber(Results.T_Profile, tankGeomData.layerVolumes', tankGeomData.V, modelParams.T_ref + 273.15, simParams.rho_w, simParams.cp_w); 
            [~, ~, ~, ~, ~, U_tank_full] = GetExergyNumber(zeros(1, tankGeomData.n)+simParams.setTemp+tankGeomData.hysteresisBand/2, tankGeomData.layerVolumes', tankGeomData.V, modelParams.T_ref + 273.15, simParams.rho_w, simParams.cp_w); 
            Results.InternalEnergy = U_tank/3600/1000;
            Results.SOC = U_tank/U_tank_full*100;
        catch err
            throw err;
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
        if(topic == updateTopic)
            updateData = jsondecode(data);
            updateReceived = true;
            disp(topic);
            disp(data);
        end      
    end
    
    %% Used to write messages to the master broker
    function publishMessage(client, topic, payload)
        write(client, topic, payload, QualityOfService = 1, Retain = true)
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



