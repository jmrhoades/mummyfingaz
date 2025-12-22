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
			"DEFAULT": 3.0,
			"MIN": 0.0,
			"MAX": 50.0
		},
		
		
		{
			"NAME": "location",
			"TYPE": "point2D",
			"DEFAULT": [
				0.5,
				0.5
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
    vec3 color = vec3(0.0);

    vec2 pos = location-st;

    float r = length(pos)*2.0;
    float a = atan(pos.y,pos.x);

    float f = cos(a*size);
     f = abs(cos(a*size));
     //f = abs(cos(a*size))*.5+.3;
     //f = abs(cos(a*12.)*sin(a*size))*.8+.1;
     //f = smoothstep(-.5,1.,cos(a*size))* 0.2 + 0.5;

    color = vec3( 1.0 - smoothstep(f,f+0.02,r) );

    gl_FragColor = vec4(color, 1.0);
}