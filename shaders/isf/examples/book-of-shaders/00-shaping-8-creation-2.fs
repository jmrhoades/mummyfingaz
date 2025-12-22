/*{
	"DESCRIPTION": "creation-2",
	"CREDIT": "Silexars via Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],
	"INPUTS": [
		{
			"NAME": "center",
			"TYPE": "float",
			"DEFAULT": 0.5,
			"MIN": 0.0,
			"MAX": 1.0
		},
		{
			"NAME": "channelOffset",
			"TYPE": "float",
			"DEFAULT": 0.07,
			"MIN": 0.0,
			"MAX": 1.0
		},
		{
			"NAME": "amp",
			"TYPE": "float",
			"DEFAULT": 9.0,
			"MIN": 0.0,
			"MAX": 20.0
		},
		{
			"NAME": "frequency",
			"TYPE": "float",
			"DEFAULT": 2.0,
			"MIN": 0.0,
			"MAX": 10.0
		},
		{
			"NAME": "mod1",
			"TYPE": "float",
			"DEFAULT": 1.0,
			"MIN": 0.0,
			"MAX": 2.0
		},
		{
			"NAME": "mod2",
			"TYPE": "float",
			"DEFAULT": 0.5,
			"MIN": 0.0,
			"MAX": 1.0
		}

	]
}*/


// Let's adapt Creation by Silexars. In this version, I've changed all the hard coded values to sliders. https://www.shadertoy.com/view/XsXXDn
// http://www.pouet.net/prod.php?which=57245

//First, we need a placeholder for TIME
float t = TIME;

//here's the main loop.
void main( ){

//here's our color vector.
	vec3 c;
//We'll divide with length.
	float l;  

//This loop is cool! It will write to each color channel individually like an array. To write in RGB, we will loop 3 times.	
	for(int i=0;i<3;i++) {
		//placeholder vector
		vec2 uv;
		//normalize
		vec2 p=gl_FragCoord.xy/RENDERSIZE;
		
		uv=p;
		
		//center
		p-=center;
		
		//offset time through the loop to get the chromatic abberation effect.
		t += channelOffset;
		p.x *=RENDERSIZE.x/RENDERSIZE.y;
		l=length(p);
		//create the sin waves
		uv+=p/l*(sin(t)+1.0)*abs(sin(l * amp - t * frequency));
		//shape them and assign to the color vector
		c[i]=.01/length(abs(mod(uv,mod1)-mod2));
	}
	//write the color vector to the FragColor
	gl_FragColor=vec4(c/l,1.0);
}
