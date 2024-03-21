function simObject = EHWST_Simulation_Start(configJsonFileUrl)
    %EHWST_Simulation_Start The calling script for the storage simulator
    %   configJsonFileUrl - The URL of the configuration file for the simulator 
    fprintf('EHWST Simulator script called!\n');
    %% Get the file name information and valide file type
    % Check whether the provided file exists
    if(~isfile(configJsonFileUrl))
        error('File does not exist')
    end
    
    [directory, ~, ext] = fileparts(configJsonFileUrl);
    if(ext ~= ".json")
        error("Config file not correct file format [JSON]")
    end
    
    % Check whether the provided directory exists
    if(~isfolder(directory))
        error('Directory does not exist')
    end
    
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
        configJson = jsondecode(str);
    catch ex
        fprintf('An error occurred: %s\n', ex.message);
        throw exception;
    end

    set(groot,'defaultAxesFontName', 'Times New Roman')
    set(groot,'defaultAxesFontSize', 11)
    set(groot,'defaultAxesFontWeight', "bold")

    %% Call the simulation class
    simObject = EHWST_Simulator(configJson);

    [T_mat_sim] = simObject.simulate();

    plot(T_mat_sim)

    % Generate some geometric plots
%     simObject.generateGeometricPlots

    % Visualise the input profiles provided in the JSON
    simObject.visualiseInputProfiles

    % Create animation if necessary
%     simObject.createAnimation(directory + '\simAnimation.avi')

fprintf('EHWST Simulator script finished!\n');
end