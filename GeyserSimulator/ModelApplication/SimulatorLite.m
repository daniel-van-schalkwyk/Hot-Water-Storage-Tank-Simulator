%% Geyser Simulator LITE
function [] = SimulatorLite(uid, settingsPath)
    
    % Read settings for geyser simulation
    settings = ini2struct(settingsPath); 

    % Define MQTT broker parameters from settings
    broker = "ssl://" + settings.ConnectionDetails.masterBrokerUrl;  
    port = int32(settings.ConnectionDetails.port);
    username = settings.ConnectionDetails.username;
    password = settings.ConnectionDetails.password;
    certificateRoot = settings.ConnectionDetails.certPath;

    % Get pub and sub topics
    global updateTopic pubTopic
    updateTopic = settings.SimulatorSubTopics.geyserUpdateTopicRoot + "/" + uid;
    pubTopic = settings.SimulatorPubTopics.geyserDataTopicRoot + "/" + uid;
    
    % Connect to MQTT master client
    client = ConnectMqtt(broker, port, uid, username, password, certificateRoot);
    
    % Subscribe to topics
    subscribe(client, updateTopic,Callback=@updateCallBackFunc)
    sprintf('Subscribed to %s', updateTopic)
    
    % Loop indefinitely to keep the listener running
    while true
        pause(1); % Adjust the pause duration as needed
    end
    
    % When done, disconnect from the broker
    disconnect(client)
    
    function [] = ListenToMqtt()
    
        % Loop indefinitely to keep the listener running
        while true
            pause(0.1); 
    
        end
    end
    
    function client = ConnectMqtt(brokerUrl, port, uid, username, password, certificateRoot)
    
        client = mqttclient(brokerUrl, Port = port, ClientID = uid, Username = username, Password = password, CARootCertificate = certificateRoot);
    
    end
    
    function updateCallBackFunc(topic, data)

        if(topic == updateTopic)
            updateData = jsondecode(data);

        end

        disp(topic);
        disp(data);
    end

end



