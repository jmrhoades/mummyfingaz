/*{
	"DESCRIPTION": "Uniforms-3",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
		{
			"NAME": "location",
			"TYPE": "point2D",
			"DEFAULT": [
				0,
				0
			]


		}
	]
}*/

//The Uniforms are either handled in the json dict above, or internally by ISF! To create the mouse location we will need to do create it in the dict and use it in our host app, which I'm assuming is VDMX. VDMX can serve up the mouse position of the whole screen, or just the canvas with a preview window as a data source. 


void main(){

	vec2 mouse = location/RENDERSIZE;
	gl_FragColor = vec4( mouse.x, mouse.y, 0.0, 1.0);
}
