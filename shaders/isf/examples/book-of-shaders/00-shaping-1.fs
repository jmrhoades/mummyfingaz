/*{
	"DESCRIPTION": "Shaping-1",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
		{

		}
	]
}*/

//Here we go, shaping functions. I've changed the variable names to be a bit more human readable for this example. First, we'll create a function for plotting lines. I've added the .02 value which is hardcoded in the example as a variable, which I think makes it easier to understand. Notice the plot function is essentiall creating a line by just slicing a very small percentage of the whole.

float plot(vec2 screensize, float percent){
	
	float linesize = 0.02;

	return smoothstep( percent - linesize, percent, screensize.y) - 
	smoothstep( percent , percent + linesize , screensize.y);
	
}


void main() {

	//create a 2D vector that normalizes the width and height and store it in a variable.
	vec2 normalized_screen = gl_FragCoord.xy/RENDERSIZE;

	//here, we create a variable "y" and reference screen width.
	
	float y = normalized_screen.x;

	// create a vector holding "y". This creates the gradient.
	vec3 color = vec3(y);

	//Next, we will plot the line and add it to the gradient. call the plot function.

	float line = plot(normalized_screen,y);

	//re-assign color to 
	color = (1.0-line) * color + line * vec3(0.0,1.0,0.0); //make the line green

	gl_FragColor = vec4(color,1.0);
}
