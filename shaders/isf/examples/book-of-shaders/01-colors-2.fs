/*{
	"DESCRIPTION": "Colors-2",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
		{

		}
	]
}*/

//Here, I'm just copying and pasting from http://patriciogonzalezvivo.com/2015/thebookofshaders/edit.html#06/easing.frag to get all those great Penner Easing Equations!



vec3 colorA = vec3(0.149,0.141,0.912);   //yellow
vec3 colorB = vec3(1.000,0.833,0.224);   ///blue


void main() {

	//This will hold our color to write to.
	vec3 color = vec3(0.0);
	
	//here's the shaping function for the percentage argument in the mix function. This is just a very simple way to oscillate between 0.0 - 1.0
	float percent = abs(sin(TIME));
	
	// Mix uses percent (a value from 0-1) to 
    // mix the two colors
    color = mix(colorA, colorB, percent);
	
	
	gl_FragColor = vec4(color,1.0);
}
