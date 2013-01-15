	attribute vec3 position;
    attribute vec3 normal;
    
    uniform mat4 viewMat;
    uniform mat4 projectionMat;
    uniform mat4 normalMat;
    uniform vec4 material;
    uniform int picking;

    varying vec3 Lighting;
    varying vec4 FragColour;

    void main(void) {
        vec4 worldPosition = viewMat * (vec4(position, 1.0));
        gl_Position = projectionMat * worldPosition;

        vec3 ambientLight = vec3(0.5, 0.5, 0.5);
        vec3 directionalLightColor = vec3(0.3, 0.3, 0.3);
        vec3 directionalVector = vec3(0.85, 0.8, 0.75);
     
        vec4 transformedNormal = normalMat * vec4(normal, 1.0);
     
        float directional = max(dot(transformedNormal.xyz, directionalVector), 0.0);
        
        Lighting = ambientLight + (directionalLightColor * directional);
        if(picking > 0) {
            Lighting = vec3(1.0, 1.0, 1.0);
			if(picking == 1) 
			   FragColour = vec4(0.0,0.0,1.0,1.0);
			if(picking == 2)
			   FragColour = vec4(0.0,1.0,1.0,1.0);
        } else {
			FragColour = material;
		}
    }
