/**
 * Cart Integration Test Script
 * 
 * Run this script in the browser console to test cart functionality
 * Make sure you're logged in as a Customer user
 */

(async function testCartIntegration() {
  console.log('🧪 Starting Cart Integration Tests...\n');
  
  const API_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8081';
  const token = localStorage.getItem('authToken');
  
  const tests = {
    passed: 0,
    failed: 0,
    total: 0,
  };
  
  function logTest(name, passed, details = '') {
    tests.total++;
    if (passed) {
      tests.passed++;
      console.log(`✅ ${name}`, details ? `- ${details}` : '');
    } else {
      tests.failed++;
      console.log(`❌ ${name}`, details ? `- ${details}` : '');
    }
  }
  
  // Test 1: Check authentication
  console.log('1. Checking authentication...');
  logTest('Authentication check', !!token, token ? 'Token found' : 'No token - will test localStorage only');
  
  // Test 2: GET cart
  console.log('\n2. Testing GET /api/customerorder/cart...');
  try {
    const getResponse = await fetch(`${API_URL}/api/customerorder/cart`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });
    
    if (getResponse.ok) {
      const data = await getResponse.json();
      logTest('GET cart', true, `Found ${data.items?.length || 0} items`);
      console.log('   Cart data:', data);
    } else {
      const errorData = await getResponse.text();
      logTest('GET cart', false, `Status: ${getResponse.status} - ${errorData}`);
    }
  } catch (error) {
    logTest('GET cart', false, error.message);
  }
  
  // Test 3: ADD item (use product ID 1 as test)
  console.log('\n3. Testing POST /api/customerorder/cart/add...');
  const testProductId = 1;
  let cartItemId = null;
  
  try {
    const addResponse = await fetch(`${API_URL}/api/customerorder/cart/add`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        productId: testProductId,
        quantity: 1,
      }),
    });
    
    if (addResponse.ok) {
      const data = await addResponse.json();
      cartItemId = data.id;
      logTest('ADD item', true, `Item ID: ${cartItemId}`);
    } else {
      const errorData = await addResponse.text();
      logTest('ADD item', false, `Status: ${addResponse.status} - ${errorData}`);
    }
  } catch (error) {
    logTest('ADD item', false, error.message);
  }
  
  // Test 4: UPDATE item
  if (cartItemId) {
    console.log('\n4. Testing PUT /api/customerorder/cart/{id}...');
    try {
      const updateResponse = await fetch(`${API_URL}/api/customerorder/cart/${cartItemId}`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          quantity: 2,
        }),
      });
      
      if (updateResponse.ok) {
        const data = await updateResponse.json();
        logTest('UPDATE item', true, `New quantity: ${data.quantity}`);
      } else {
        const errorData = await updateResponse.text();
        logTest('UPDATE item', false, `Status: ${updateResponse.status} - ${errorData}`);
      }
    } catch (error) {
      logTest('UPDATE item', false, error.message);
    }
  }
  
  // Test 5: REMOVE item
  if (cartItemId) {
    console.log('\n5. Testing DELETE /api/customerorder/cart/{id}...');
    try {
      const deleteResponse = await fetch(`${API_URL}/api/customerorder/cart/${cartItemId}`, {
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json',
        },
      });
      
      if (deleteResponse.ok) {
        logTest('REMOVE item', true);
      } else {
        const errorData = await deleteResponse.text();
        logTest('REMOVE item', false, `Status: ${deleteResponse.status} - ${errorData}`);
      }
    } catch (error) {
      logTest('REMOVE item', false, error.message);
    }
  }
  
  // Test 6: localStorage
  console.log('\n6. Testing localStorage...');
  const localCart = localStorage.getItem('cart');
  if (localCart) {
    try {
      const parsed = JSON.parse(localCart);
      logTest('localStorage cart', true, `${parsed.length} items`);
    } catch (error) {
      logTest('localStorage cart', false, 'Corrupted data');
    }
  } else {
    logTest('localStorage cart', true, 'Empty (expected if authenticated)');
  }
  
  // Test 7: Error handling
  console.log('\n7. Testing error handling...');
  try {
    const errorResponse = await fetch(`${API_URL}/api/customerorder/cart/99999`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json',
      },
    });
    
    if (errorResponse.status === 404) {
      logTest('Error handling (404)', true, 'Correctly handled not found');
    } else {
      logTest('Error handling (404)', false, `Unexpected status: ${errorResponse.status}`);
    }
  } catch (error) {
    logTest('Error handling', false, error.message);
  }
  
  // Summary
  console.log('\n' + '='.repeat(50));
  console.log('📊 Test Summary:');
  console.log(`   Total: ${tests.total}`);
  console.log(`   ✅ Passed: ${tests.passed}`);
  console.log(`   ❌ Failed: ${tests.failed}`);
  console.log(`   Success Rate: ${((tests.passed / tests.total) * 100).toFixed(1)}%`);
  console.log('='.repeat(50));
  
  if (tests.failed === 0) {
    console.log('🎉 All tests passed!');
  } else {
    console.log('⚠️  Some tests failed. Check the details above.');
  }
})();

