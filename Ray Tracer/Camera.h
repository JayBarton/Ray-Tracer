#pragma once
#include <glm.hpp>
#include <gtc/matrix_transform.hpp>

class Camera
{
public:
	Camera();
	~Camera();


	glm::mat4 getMatrix()
	{
		if (update)
		{
			update = false;
			matrix = glm::lookAt(position, position + front, glm::vec3(0.0f, 1.0f, 0.0f));
			return matrix;
		}
		else
		{
			return matrix;
		}
	}

	glm::vec3 getPosition()
	{
		return position;
	}

	glm::vec3 getFront()
	{
		return front;
	}

	glm::vec2 Rot()
	{
		return glm::vec2(xRotation, yRotation);
	}

	void set(glm::vec3 p, glm::vec3 f);

	void getInput(unsigned char key);

private:
	glm::vec3 position;
	glm::vec3 front;
	glm::vec3 side;

	glm::mat4 matrix;

	bool update = true;

	float xRotation = -90.0f;	
	float yRotation = 0.0f;
};

