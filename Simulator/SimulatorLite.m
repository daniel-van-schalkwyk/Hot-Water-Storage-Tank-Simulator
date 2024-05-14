%% Geyser Simulator LITE
function [] = StartGeyserSimInstance(broker, port, uid, username, password)
    
% Define MQTT broker parameters
broker = 'ssl://f5c5e1b086a347bd8bce213e41b955c1.s1.eu.hivemq.cloud';  
port = 8883;             % MQTT topic to subscribe to
uid = 'U001';
username = 'geyserSim';
password = 'Geyser4you!';
certificateRoot = 'C:\Users\DanielvanSchalkwyk\OneDrive - Stellenbosch University\PhD\EWH simulator\GeyserSimulator\Settings\HiveMQ_cert.pem';
updateTopic = "GeyserSim/"+uid+"/Update";
pubTopic = "GeyserSim/"+uid+"/Data";

client = ConnectMqtt(broker, port, uid, username, password, certificateRoot);

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
    disp(topic);
    disp(data);
end



