@echo off

SET /A x = 110
SET /A y = 80

for %%c in ("NoAA" "AA-Morphological" "FXAA" "SMAA") do (
	convert %%c.png -crop 64x64+%x%+%y% %%c_cropped.png
	convert %%c_cropped.png -interpolate Integer -filter point -resize "400%%" %%c_resized.png
)

@pause

