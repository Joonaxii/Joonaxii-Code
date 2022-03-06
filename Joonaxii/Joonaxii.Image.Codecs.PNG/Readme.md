# PNG Encoder/Decoder
Uses .NET Framework 4.7 <br>
Only uses libraries already included in .NET Framework and other solutions from this repository

## Required Libraries (From this repository)
  -Joonaxii.Collections <br>
  -Joonaxii.Data <br>
  -Joonaxii.Data.Coding <br>
  -Joonaxii.Image <br>
  -Joonaxii.Image.Codecs <br>
  -Joonaxii.Image.Texturing <br>
  -Joonaxii.IO <br>
  -Joonaxii.MathJX <br>

# Decoder
### Supports
 -Images with or without palette <br>
 -Up to a bit depth of 16 <br>
 -Images with transparency <br>
 
### Does NOT Support
 -Interlaced images <br>
 -Images with bit depth lower than 8 <br>
 -Ignores metadata chunks like text etc <br>
 
 
# Encoder
### Supports
 -Palette <br>
 -"Illegal" palette sizes <br>
 -"Adaptive" pre-filtering <br>
 
### Does NOT Support
 -Interlacing <br>
 -Bit depths other than 8 bits (except for "illegal" palette indices) <br>
 -Metadata chunks like text etc
 
 
