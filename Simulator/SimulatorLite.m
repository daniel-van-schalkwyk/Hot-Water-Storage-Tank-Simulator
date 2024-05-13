%% Geyser Simulator LITE
    
    % Define MQTT broker parameters
    broker = 'ssl://f5c5e1b086a347bd8bce213e41b955c1.s1.eu.hivemq.cloud';  % Replace this with your MQTT broker address
    topic = '#';             % MQTT topic to subscribe to
    
    % Create MQTT client
    mqClient = mqttclient(broker, Port = 8883, ClientID = 'Daniel', Username = 'geyserSim', Password = 'Geyser4you!', CARootCertificate = 'C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\GeyserSimulator\Settings\hiveMQ_cert.crt');
    
    
    % Loop indefinitely to keep the listener running
    while true
        pause(1); % Adjust the pause duration as needed
    end
    
    % When done, disconnect from the broker
    disconnect(client)

