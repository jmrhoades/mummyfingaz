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
			"DEFAULT": 0.1,
			"MIN": 0.0,
			"MAX": 0.5
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

//Let's package all this functionality up into a function. Using smoothstep will give smooth edges. 

float circle(in vec2 _st, in float _radius){
    vec2 l = _st-vec2(location);
	return 1.-smoothstep(_radius-(_radius*0.01),
                         _radius+(_radius*0.01),
                         dot(l,l)*4.0);
}



void main(){
	
	
    vec2 st = gl_FragCoord.xy/RENDERSIZE;

    vec3 color = vec3(0.0);
    
    float pct = circle(st,size);
    
    color = vec3(pct);
    
    gl_FragColor = vec4(color,1.0);
}
