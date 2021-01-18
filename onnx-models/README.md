# Test onnx model creation of efficientnet
- Using Tensorflow 2.3.1, tf2onnx 1.7.2, and Keras 2.4.3
- https://github.com/onnx/keras-onnx/issues/557

```python
import tensorflow as tf

# Create the model with default imagenet weights
EffnetB0 = tf.keras.applications.EfficientNetB0(
     include_top=True,
     weights='tf-efficientnet-weights/efficientnetb0.h5')

# Save the model
saved_model_dir = "efficientnetb0"
tf.saved_model.save(EffnetB0, saved_model_dir)
```
- Channels first for keras models **NCWH**, OpenCV reads in **NWHC** 
- Open a terminal window and enter the following command

```bash
python -m tf2onnx.convert --saved-model efficientnetb0 --opset 9 --output efficientnetb0.onnx --fold_const --inputs-as-nchw input_2:0
```
- Onnx model should have successfully converted, open in WinMlDashboard and ensure that the model appears correctly and is able to be run using the tool
- To change the naming convention of the onnx model from `input_2:0` and `Identity:0` to `input_2` and `Identity` 
- https://github.com/onnx/onnx/issues/2052 

```python
import onnx

# Fill model name and input/output layer name fields
onnx_model = onnx.load('onnx-models/efficientnetb0.onnx')
endpoint_names = ['input_2:0', 'Identity:0']

for i in range(len(onnx_model.graph.node)):
	for j in range(len(onnx_model.graph.node[i].input)):
		if onnx_model.graph.node[i].input[j] in endpoint_names:
			print('-'*60)
			print(onnx_model.graph.node[i].name)
			print(onnx_model.graph.node[i].input)
			print(onnx_model.graph.node[i].output)

			onnx_model.graph.node[i].input[j] = onnx_model.graph.node[i].input[j].split(':')[0]

	for j in range(len(onnx_model.graph.node[i].output)):
		if onnx_model.graph.node[i].output[j] in endpoint_names:
			print('-'*60)
			print(onnx_model.graph.node[i].name)
			print(onnx_model.graph.node[i].input)
			print(onnx_model.graph.node[i].output)

			onnx_model.graph.node[i].output[j] = onnx_model.graph.node[i].output[j].split(':')[0]

for i in range(len(onnx_model.graph.input)):
	if onnx_model.graph.input[i].name in endpoint_names:
		print('-'*60)
		print(onnx_model.graph.input[i])
		onnx_model.graph.input[i].name = onnx_model.graph.input[i].name.split(':')[0]

for i in range(len(onnx_model.graph.output)):
	if onnx_model.graph.output[i].name in endpoint_names:
		print('-'*60)
		print(onnx_model.graph.output[i])
		onnx_model.graph.output[i].name = onnx_model.graph.output[i].name.split(':')[0]

onnx.save(onnx_model, 'onnx-models/efficientnetb0_mod.onnx')
``` 

