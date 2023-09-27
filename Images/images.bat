SET /A x = 110 
SET /A y = 80 


convert SMAA.png -crop 64x64+%x%+%y% SMAA_cropped.png

convert FXAA.png -crop 64x64+%x%+%y% FXAA_cropped.png

convert NoAA.png -crop 64x64+%x%+%y% NoAA_cropped.png

convert AA-Morphological.png -crop 64x64+%x%+%y% AA-Morphological_cropped.png

convert SMAA_cropped.png -interpolate Integer -filter point -resize "400%" SMAA_resized.png

convert FXAA_cropped.png -interpolate Integer -filter point -resize "400%" FXAA_resized.png

convert NoAA_cropped.png -interpolate Integer -filter point -resize "400%" NoAA_resized.png

convert AA-Morphological_cropped.png -interpolate Integer -filter point -resize "400%" AA-Morphological_resized.png
