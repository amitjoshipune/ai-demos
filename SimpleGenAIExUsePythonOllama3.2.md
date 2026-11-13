:SetEnvironmentVariable( "OLLAMA_HOST", "0.0.0.0:11434", "Machine" )

curl http://192.168.1.5:11434/api/generate -d '{   "model":"llama3.2:latest",   "prompt":"hello from ubuntu",   "stream":false }'

"Here's a step-by-step guide on how to create a GenAI model example using Python and OlLaMA-3.2 on your Windows Dell laptop:

**Prerequisites**
1. Install Python 3.9 or later on your system.
2. Install the `transformers` library using pip: `pip install transformers`
3. Download the OlLaMA-3.2 model (version 3.2.0 or later) from the official Hugging Face model hub. Save the model to a folder named `ollama3_3.2_model`.
4. Make sure the `ollama3_3.2_model` folder is in the same directory as your Python script

**Quick GenAI Example using OlLaMA-3.2**

Create a new Python file (e.g., `genai_example.py`) and add the following code:
```python\nimport torch
from transformers import OlLaMAForConditionalGeneration

# Load the OlLaMA-3.2 model\nmodel = OlLaMAForConditionalGeneration.from_pretrained('ollla/ollama3_3.2_model')

# Set the device (GPU or CPU)\ndevice = torch.device('cuda' if torch.cuda.is_available() else 'cpu')\nmodel.to(device)

# Define a function to generate text
def generate_text(input_text, max_length=100):
    input_ids = model.encode(input_text, return_tensors='pt').to(device)
    output = model.generate(input_ids, max_length=max_length)
	    return output
		
		# Test the model\ninput_text = \"Hello, how are you?\"\ngenerated_text = generate_text(input_text)\nprint(generated_text)
```
Run the Script**

Run the script using Python: `python genai_example.py`

This script loads the OlLaMA-3.2 model, sets the device (GPU or CPU), and defines a function to generate text. The `generate_text` function takes an input text and returns a generated text of the same length.

**Tips and Variations**

* To use the model on a GPU, make sure it's installed on your system and the driver is up-to-date.
* You can adjust the `max_length` parameter to control the length of the generated text.
* You can add more complexity to the model by using additional input features or modifying the model architecture.
* You can use this model as a starting point for more advanced GenAI tasks, such as sequence generation or text classification.

That's it! With this example, you've created a quick GenAI model example using Python and OlLaMA-3.2 on your Windows Dell laptop."