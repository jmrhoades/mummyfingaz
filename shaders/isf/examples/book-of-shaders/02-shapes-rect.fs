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

//Let's build our square. 


void main(){
	
	
    vec2 st = gl_FragCoord.xy/RENDERSIZE;

    vec3 color = vec3(0.0);
    
    // Each result will return 1.0 (white) or 0.0 (black).
   
    //float left = step(0.1,st.x);   // Similar to ( X greater than 0.1 )
    //float bottom = step(0.1,st.y); // Similar to ( Y greater than 0.1 )
    
    //The multiplication of left*bottom will be similar to the logical AND. This works because we are working with normalized values. Try changing this to + to see the realtionship created with arithmetic operators.
   
    //color = vec3( left * bottom ); 
    
    //This could also be written as:
    
    vec2 size = vec2(size);
    
    vec2 bl = step(size,st);
    
    float pct = bl.x * bl.y;
    
    //Notice I've replaced the hard coded 0.1 value with our slider reference!
    
    //Now, let's do the upper right. Normalized values are great, simply subtract from 1.0 to get the inverse.
    
    vec2 tr = step(size,1.0-st);
    
    //Note, this could also be written as pct = bl.x * bl.y * tr.x * tr.y;
    
    pct *= tr.x * tr.y;
    
    color = vec3(pct);


    gl_FragColor = vec4(color,1.0);
}
