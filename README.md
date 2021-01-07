# HoloLens-2-Machine-Learning
Using the `EfficientNetB0` model, trained on the `ImageNet` 1000 class dataset, for image classification. Model inference is run directly on the HoloLens 2 using its onboard CPU. 
- Tested with Unity 2019.4 LTS and the HoloLens 2
- Building off of the [WinMLExperiments](https://github.com/reneschulte/WinMLExperiments) sample from Rene Schulte
- Input video frames of size `(224, 224)` for online inference
- Pretrained TensorFlow-Keras implementation of the EfficientNetB0 framework was converted directly to ONNX format for use in this sample

## Build sample
- Open sample in Unity
- Switch build platform to `Universal Windows Platform`, select `HoloLens` for target device, and `ARM64` as the target platform
- Build Visual Studio project and open .sln file
- Add the `model.onnx` file as an existing file to the project under the assets folder
- In the asset properties window (as below), confirm that the `Content` field has its boolean value set to `True`. This enables the `ONNX` model to be loaded at runtime from the Visual Studio assets folder

![](onnx-model-load.PNG)

- Deploy the sample to the HoloLens and test. Prediction labels are pulled from the parsed ImageNet labels .json file
- Output includes the prediction label and associated probability (%) as well as the inference time in milliseconds 

![](french-bulldog-detection.jpg)