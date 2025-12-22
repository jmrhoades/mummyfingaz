/*{
	"DESCRIPTION": "Shaping-4",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
		{
			"NAME": "level",
			"TYPE": "float",
			"DEFAULT": 5.0,
			"MIN": 0.0,
			"MAX": 20.0
		}
	]
}*/

//Here's Smoothstep or easing.

float plot(vec2 screensize, float percent){
	
	float linesize = 0.02;

	return smoothstep( percent - linesize, percent, screensize.y) - 
	smoothstep( percent , percent + linesize , screensize.y);
	
}


void main() {

	//create a 2D vector that normalizes the width and height and store it in a variable.
	vec2 normalized_screen = gl_FragCoord.xy/RENDERSIZE;

	//here, we create a variable "y" and reference screen width.
	
	float y = smoothstep(0.2,0.5,normalized_screen.x) - smoothstep(0.5,0.8,normalized_screen.x);

	// create a vector holding "y". This creates the gradient.
	vec3 color = vec3(y);

	//Next, we will plot the line and add it to the gradient. call the plot function.
	float line = plot(normalized_screen,y);

	//re-assign color to 
	color = (1.0-line) * color + line * vec3(0.0,1.0,0.0); //make the line green

	gl_FragColor = vec4(color,1.0);
}
