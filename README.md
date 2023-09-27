# Unity Morphological Antialiasing

_This is intended as a personal study and a work in progress, some remaining tasks are mentioned below. However you should be able to try it out, use it as it is or improve it yourself. Tested with DirectX11, although we expect compatibility with URP supported platforms._

This is an implementation of **Morphological Antialiasing** for **Unity2022.3** and **URP** based on the article "Practical Morphological Antialiasing" from the [GPU Pro 360 Guide to Rendering
](https://www.routledge.com/GPU-Pro-360-Guide-to-Rendering/Engel/p/book/9780815365501) book. The main strength of the technique is its cheap cost compared to obtained results. We will not repeat the contents of the article here, and recommend checking it out.

| No Antialiasing | Our Implementation |
|---|---|
| ![No Antialiasing](./Images/cubes_no_AA.png) | ![Our Implementation](./Images/cubes_with_AA.png) |

The technique comprises 3 full-screen steps:

* Edge detection.
* Calculation of blending weights.
* Blending with neighborhood.

## Usage

* Add the "Morphological Antialiasing" renderer feature to your renderer asset, at the bottom of the inspector.
* Make sure the camera has a depth buffer.
* Make sure the camera's antialiasing is set to None (as using multiple antialiasing techniques simultaneously hardly makes sense).

### Renderer Feature Settings

* **Render Pass Event**, the event scheduling the pass within frame rendering. Typically you'd use "BeforeRenderingPostProcessing". By that point, no subsequent step should create "jaggies", and post process effects may interfere with edge detection.
* **Intermediate Buffer Type**, lets you select which of the technique's 3 steps you'd like to visualize. For example, looking at the edges buffer may help adjusting threshold.
* **Edge Detect Mode**, the method used for edge detection. We implement both depth and luminance based edge detection. So far we obtained better results with depth. Luminance based edge detection tends to struggle to isolate relevant edges wich may lead to artefacts and unnecessary processing of many pixels.
* **Threshold**, the threshold used for edge detection.
* **Max Distance**, the maximal distance in pixels used for pattern detection.

Typically, higher maximal distances (up to a plateau) will lead to better results, at the cost of extra GPU computations. It should be tuned according to the nature of the content and performance budget.

## Implementation

The technique is implemented as a `ScriptableRendererFeature` named `MorphologicalAntialiasing`. It uses one pass named `MorphologicalAntialiasingPass`. It uses 3 shaders, one for each of the 3 full-screen steps. Those are very close to the examples provided in the book, only adapted for integration with Unity/URP. Shaders are split in a `.shader` and `.hlsl` file. This lets us separate shaderlab properties and classic shader code. It also makes for cleaner code when implementing multiple passes.

The technique uses a lookup texture storing pixel coverages for all handled patterns. We implemented the generation of this texture in `AreaLookup`. It is recalculated whenever **Max Distance** changes. We provide a simple `TestAreaLookup` component to test this lookup generation in isolation.

Below is a view of the intermediate buffers used.

| Edges | Stencil | Blending Weights |
|---|---|---|
| ![Edges](./Images/edges.png) | ![Stencil](./Images/stencil.png) | ![Blending Weights](./Images/blending-weights.png) |

### Possible Improvements

So far, our edge detection technique requires improvements. We mentioned that depth gives more reliable results than luminance. Yet, it fails to catch some relevant edges. We could also improve the coverage lookup texture generation for better blending between patterns.

## Results

| No Antialiasing | Our Implementation |
|---|---|
| ![No Antialiasing](./Images/NoAA_resized.png) | ![Our Implementation](./Images/AA-Morphological_resized.png) |
| URP FXAA | URP SMAA |
| ![URP FXAA](./Images/FXAA_resized.png) | ![URP SMAA](./Images/SMAA_resized.png) |