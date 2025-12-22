/*{
	"DESCRIPTION": "2D Matrices",
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
		},
		{
			"NAME": "location",
			"TYPE": "point2D",
			"DEFAULT": [
				0.5,
				0.5
			],
			
			"MIN": [
				0.0,
				0.0
			],
			
			"MAX": [
				1.0,
				1.0
			]
		}
	]
}*/

//Two functions to build a box and cross.


float box(in vec2 _st, in vec2 _size){
    _size = vec2(0.5) - _size*0.5;
    vec2 uv = smoothstep(_size, _size+vec2(0.001), _st);
    uv  *= smoothstep(_size, _size+vec2(0.001), vec2(1.0) - _st);
    return uv.x * uv.y;
}



float cross(in vec2 _st, float _size){
    return  box(_st, vec2(_size,_size/4.)) + 
            box(_st, vec2(_size/4.,_size));
}



void main(){
    vec2 st = gl_FragCoord.xy/RENDERSIZE;
    vec3 color = vec3(0.0);
        
    // To move the cross we move the space. Uncomment for the location control.

    //vec2 translate = (location - 0.5);
    //st -= translate;
    
    vec2 translate = vec2(cos(TIME),sin(TIME));
    st += translate*.3;

    // Show the coordinates of the space on the background
     color = vec3(st.x,st.y,0.0);

    // Add the shape on the foreground
    color += vec3(cross(st,size));

    gl_FragColor = vec4(color,1.0);
}


