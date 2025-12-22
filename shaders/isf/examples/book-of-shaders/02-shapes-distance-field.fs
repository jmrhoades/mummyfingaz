/*{
	"DESCRIPTION": "shapes",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
		{
			"NAME": "size",
			"TYPE": "float",
			"DEFAULT": 10,
			"MIN": 0.0,
			"MAX": 50.0
		},
		
		
		{
			"NAME": "location",
			"TYPE": "point2D",
			"DEFAULT": [
				0.3,
				0.3
			],
			
			"MIN": [
				0.0,
				0.0
			],
			
			"MAX": [
				1.0,
				1.0
			]
		}
	]
			
}*/



void main(){
  vec2 st = gl_FragCoord.xy/RENDERSIZE;
  st.x *= RENDERSIZE.x/RENDERSIZE.y;
  vec3 color = vec3(0.0);
  float d = 0.0;

  // Remap the space to -1. to 1.
  st = st * 2.-1.;

  // Make the distance field
  d = length( abs(st)-location );
  // d = length( min(abs(st)-.3,0.) );
  // d = length( max(abs(st)-.3,0.) );

  // Visualize the distance field
  gl_FragColor = vec4(vec3(fract(d*abs(sin(TIME)*size))),1.0);

  // Drawing with the distance field
  // gl_FragColor = vec4(vec3( step(.3,d) ),1.0);
  // gl_FragColor = vec4(vec3( step(.3,d) * step(d,.4)),1.0);
  // gl_FragColor = vec4(vec3( smoothstep(.3,.4,d)* smoothstep(.6,.5,d)) ,1.0);
}