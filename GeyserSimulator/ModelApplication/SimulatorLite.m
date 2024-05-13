%% Geyser Simulator LITE
function [] = SimulatorLite(uid, settingsPath, configPath)
    
    % Read settings for geyser simulation
    settings = ini2struct(settingsPath); 
    
    % Define MQTT broker parameters from settings
    broker = "ssl://" + settings.ConnectionDetails.masterBrokerUrl;  
    port = int32(settings.ConnectionDetails.port);
    username = settings.ConnectionDetails.username;
    password = settings.ConnectionDetails.password;
    certificateRoot = settings.ConnectionDetails.certPath;

    % Get pub and sub topics
    global updateTopic pubDataTopic pubInfoTopic updateData updateReceived
    updateTopic = settings.SimulatorSubTopics.geyserUpdateTopicRoot + "/" + uid;
    pubDataTopic = settings.SimulatorPubTopics.geyserDataTopicRoot + "/" + uid;
    pubInfoTopic = settings.SimulatorPubTopics.geyserInfoTopicRoot + "/" + uid;
    
    % Connect to MQTT master client
    client = ConnectMqtt(broker, port, uid, username, password, certificateRoot);
    
    % Subscribe to topics
    subscribe(client, updateTopic, Callback=@updateCallBackFunc)
    sprintf('Subscribed to %s', updateTopic)
    
    % Import config data 
    configData = importConfigData(configPath);

    % Generate the Geometric data for the tank
    tankGeom = generateTankGeom(configData);

    % Loop indefinitely to keep the listener running
    ListenToMqtt(client, configData, tankGeom)
    
    % When done, disconnect from the broker
    disconnect(client)

    %% Functions
    function [] = ListenToMqtt(client, configData, tankGeomData)
        % Loop indefinitely to keep the listener running
        while true
            if(updateReceived)
                % Acknowledge message
                ackMessage.Description = "ACK";
                publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
                updateReceived = false;
                
                % Interpret the update message
                try
                    % Run model with provided inputs
                    Results = GeyserModel(updateData, configData, tankGeomData);
                    % Publish results
                    publishMessage(client, pubDataTopic, jsonencode(Results))
                catch err
                    ackMessage.Description = "Error: " + err.message;
                    publishMessage(client, pubInfoTopic, jsonencode(ackMessage))
                end
            end

            % Wait 5ms
            pause(0.005); 
    
        end
    end
    %%
    function client = ConnectMqtt(brokerUrl, port, uid, username, password, certificateRoot)
        % Method used to connect to the broker using connection information
        % from the settings file.
        client = mqttclient(brokerUrl, Port = port, ClientID = uid, Username = username, Password = password, CARootCertificate = certificateRoot);
    
    end
    %%
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
    %%
    function publishMessage(client, topic, payload)
        write(client, topic, payload, QualityOfService = 1, Retain = true)
    end

    function Results = GeyserModel(geyserData)
        % The model that is executed and that is used to generate the
        % results
        
        Results.Description = "Model execution triggered";
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
    function tankGeom = generateTankGeom(configJson)
        try 
            % Get geometric attributes
            tankGeom.t = configJson.geyser.tWall;                               
            tankGeom.L = configJson.geyser.longLength;                          
            tankGeom.R = configJson.geyser.diameter/2 - tankGeom.t;
            tankGeom.n = configJson.modelParameters.nodeNumber;
            tankGeom.orientation = configJson.geyser.orientation;
            tankGeom.layerConfig = configJson.geyser.layerConfig;
            tankGeom.h_thermostat_rel = configJson.geyser.h_thermistor_rel;
            
            % Populate the more advanced geometric characteristics
            tankGeom = EwhGeometryTools.populateTankGeometry(tankGeom);
        catch ex
            fprintf('An error occurred: %s\n', ex.message);
            throw ex;
        end
    end
end



