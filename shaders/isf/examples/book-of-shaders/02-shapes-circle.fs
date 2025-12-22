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
		}
	]
}*/

//This compares the various calls to get the distance from a Pixel.


void main(){
	
	
    vec2 st = gl_FragCoord.xy/RENDERSIZE;

    vec3 color = vec3(0.0);
    
    float pct = 0.0;

    // a. The DISTANCE from the pixel to the center
    pct = distance(st,vec2(0.5));

    // b. The LENGTH of the vector 
    //    from the pixel to the center 
    // vec2 toCenter = vec2(0.5)-st;
    // pct = length(toCenter);

    // c. The SQUARE ROOT of the vector 
    //    from the pixel to the center 
    // vec2 tC = vec2(0.5)-st;
    // pct = sqrt(tC.x*tC.x+tC.y*tC.y);

	color = vec3(pct);
    gl_FragColor = vec4(color,1.0);
}
