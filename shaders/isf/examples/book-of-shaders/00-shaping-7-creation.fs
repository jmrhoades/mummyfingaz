/*{
	"DESCRIPTION": "creation",
	"CREDIT": "Silexars via Patricio Gonzalez Vivo translated by @colin_movecraft",
	"CATEGORIES": [
		"TEST"
	],

	"INPUTS": [
	]
}*/


// Let's adapt Creation by Silexars. https://www.shadertoy.com/view/XsXXDn
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
		p-=0.5;
		
		//offset time through the loop to get the chromatic abberation effect.
		t += .07;
		p.x *=RENDERSIZE.x/RENDERSIZE.y;
		l=length(p);
		//create the sin waves
		uv+=p/l*(sin(t)+1.0)*abs(sin(l*9.-t*2.));
		//shape them and assign to the color vector
		c[i]=.01/length(abs(mod(uv,1.)-.5));
	}
	//write the color vector to the FragColor
	gl_FragColor=vec4(c/l,1.0);
}
