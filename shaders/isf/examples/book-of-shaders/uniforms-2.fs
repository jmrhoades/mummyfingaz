/*{
	"DESCRIPTION": "Uniforms-2",
	"CREDIT": "Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
	],
	"INPUTS": [

	]
}*/

//The Uniforms are either handled in the json dict above, or internally by ISF! Check it out, here's RENDERSIZE, which is th esame as u_resolution or Canvas size in width and height.



void main()
{
	vec2 screensize = gl_FragCoord.xy/RENDERSIZE;
	gl_FragColor = vec4(screensize.x,screensize.y,0.0,1.0);
}
