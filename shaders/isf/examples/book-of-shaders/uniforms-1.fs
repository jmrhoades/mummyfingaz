/*{
	"DESCRIPTION": "Uniforms-1",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
	],
	"INPUTS": [

	]
}*/

//The Uniforms are either handled in the json dict above, or internally by ISF! Check it out, TIME is a float type and works similarly to iGlobalTime in shadertoy or u_time in Book of Shaders.


void main()
{
	gl_FragColor = vec4( abs(sin(TIME)),0,0,1 );
}
