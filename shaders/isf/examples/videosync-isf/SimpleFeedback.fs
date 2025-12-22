/*{
	"DESCRIPTION": "RGB GLitchMod",
	"CREDIT": "by dantheman",
	"CATEGORIES": [
		"Distortion Effect"
	],
	"INPUTS": [
		{
			"NAME": "inputImage",
			"TYPE": "image"
		},
		{
			"NAME": "offset",
			"TYPE": "float",
			"MIN":0.0,
			"MAX":0.5,
			"DEFAULT": 0.05
		},
		{
			"NAME": "offset_right",
			"TYPE": "float",
			"MIN":0.0,
			"MAX":0.1,
			"DEFAULT": 0
		},
		{
		  "NAME": "mix_var",
		  "TYPE":"float",
		  "MIN":0.0,
			"MAX":1.0,
			"DEFAULT": 1.0
		}
  ],

	"PASSES": [
		{
			"TARGET":"one",
			"WIDTH": "$WIDTH",
			"HEIGHT": "$HEIGHT",
			"DESCRIPTION": "buffer",
			"PERSISTENT": true
		}
	]

}*/


void main() {

  vec2 pos = isf_FragNormCoord;
  vec2 newPos = vec2(pos.x + offset_right, pos.y + offset);
  vec4 old = IMG_NORM_PIXEL(one, newPos);
  vec4 new = IMG_NORM_PIXEL(inputImage, pos);

  gl_FragColor = (new  + old * mix_var * 10.0) / (1.0 + mix_var * 10.0);
}
