from fastapi.testclient import TestClient
from main import app

client = TestClient(app)


def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json() == {
        "status": "alive",
        "message": "AI Engine is ready.",
    }


def test_swagger_endpoints_in_dev_mode():
    response = client.get("/docs")
    assert response.status_code == 200

    response_openapi = client.get("/openapi.json")
    assert response_openapi.status_code == 200
    assert response_openapi.json()["info"]["title"] == "OptiLifts AI Engine"


def test_swagger_endpoints_disabled_in_production(monkeypatch):
    import importlib
    import main

    monkeypatch.setenv("ENVIRONMENT", "production")
    importlib.reload(main)
    prod_client = TestClient(main.app)

    response_docs = prod_client.get("/docs")
    assert response_docs.status_code == 404

    response_openapi = prod_client.get("/openapi.json")
    assert response_openapi.status_code == 404

    # Reset back to dev
    monkeypatch.setenv("ENVIRONMENT", "development")
    importlib.reload(main)
