// Adapted from the WinML MNIST sample 
// https://github.com/microsoft/Windows-Machine-Learning/tree/master/Samples/MNIST

#if ENABLE_WINMD_SUPPORT
using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;

public sealed class CustomNetworkInput
{
    public ImageFeatureValue features; // (3, 224, 224)
}

public sealed class CustomNetworkOutput
{
    public TensorFloat prediction; // (1000)
}

public sealed class CustomNetworkModel
{
    private LearningModel model;
    private LearningModelSession session;
    private LearningModelBinding binding;
    public static async Task<CustomNetworkModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
    {
        // Run on the GPU
        //var device = new LearningModelDevice(LearningModelDeviceKind.DirectX);

        CustomNetworkModel learningModel = new CustomNetworkModel();
        learningModel.model = await LearningModel.LoadFromStreamAsync(stream);
        //learningModel.session = new LearningModelSession(learningModel.model, device);
        learningModel.session = new LearningModelSession(learningModel.model);
        learningModel.binding = new LearningModelBinding(learningModel.session);
        return learningModel;
    }

    public async Task<CustomNetworkOutput> EvaluateAsync(CustomNetworkInput input)
    {
        // Ensure the input and output fields are bound to the correct
        // layer names in the onnx model
        binding.Bind("input_2", input.features);
        var result = await session.EvaluateAsync(binding, "0");
        var output = new CustomNetworkOutput();
        output.prediction = result.Outputs["Identity"] as TensorFloat;
        return output;
    }
}

#endif
