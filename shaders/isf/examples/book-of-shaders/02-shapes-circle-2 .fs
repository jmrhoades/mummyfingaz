/*{
	"DESCRIPTION": "Shapes",
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
		}
	]
}*/

//Let's hoook up our circle to the slider


void main(){
	
	
    vec2 st = gl_FragCoord.xy/RENDERSIZE;

    vec3 color = vec3(0.0);
    
    float pct = 0.0;

    pct = distance(st,vec2(0.5));
    
    //Here we call step and cut off at our controllable value.
    
    pct = step(pct,size);

	color = vec3(pct);
	
    gl_FragColor = vec4(color ,1.0);
}
