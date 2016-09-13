namespace datNET

module Validations =
    open System.IO

    let private _configurationFailedMessage propertyName =
        sprintf "No %s specified in datNET configuration, and automatic detection failed." propertyName

    let private _raiseFileNotFoundException propertyName =
        let errorMessage = _configurationFailedMessage propertyName
        raise(FileNotFoundException(errorMessage))

    let private _fileExists filePath propertyName =
        match File.Exists(filePath) with
        | true -> filePath
        | false -> _raiseFileNotFoundException propertyName

    let EnsureConfigPropertyFileExists propertyName configPropertyFilePath  =
        match configPropertyFilePath with
        | Some filePath -> _fileExists filePath propertyName
        | None -> _raiseFileNotFoundException propertyName
