var superagent = require('superagent')
var expect = require('expect.js')
var app = require('./app.js')
let baseUrl = 'http://localhost:3007/api'
before(function(done){
  app.listen(3007, done)
})
describe('express rest api server', function(){
  var id

  it('posts an object', function(done){
    superagent.post(`${baseUrl}/posts`)
      .send({ author: 'John',
        text: `There's a better alternative to the ubiquitous JSON as the communication protocol of the web. It's Protocol Buffers (protobuf). In a nutshell, protobuf offers a more dense format (faster processing) and provides data schemas (enforcement of structure and better compatibility with old code). `
      })
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(res.body).to.be.number
        expect(res.body.id).to.be.number
        id = res.body.id
        done()
      })
  })

  it('retrieves an object', function(done){
    superagent.get(`${baseUrl}/posts/`+id)
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(typeof res.body).to.eql('object')
        expect(res.body.id).to.be.number
        expect(res.body.id).to.eql(id)
        expect(res.body.author).to.eql('John')
        done()
      })
  })

  it('retrieves a collection', function(done){
    superagent.get(`${baseUrl}/posts`)
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(res.body.length).to.be.above(0)
        expect(res.body.map(function (item){return item.id})).to.contain(id)
        done()
      })
  })

  it('updates an object', function(done){
    superagent.put(`${baseUrl}/posts/`+id)
      .send({author: 'Peter', id: id})
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(typeof res.body).to.eql('object')
        expect(res.body.msg).to.eql('success')
        done()
      })
  })

  it('checks an updated object', function(done){
    superagent.get(`${baseUrl}/posts/`+id)
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(typeof res.body).to.eql('object')
        expect(res.body.id).to.eql(id)
        expect(res.body.author).to.eql('Peter')
        done()
      })
  })
  it('removes an object', function(done){
    superagent.del(`${baseUrl}/posts/`+id)
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(typeof res.body).to.eql('object')
        expect(res.body.msg).to.eql('success')
        done()
      })
  })
  it('checks an removed object', function(done){
    superagent.get(`${baseUrl}/posts/`)
      .end(function(e, res){
        // console.log(res.body)
        expect(e).to.eql(null)
        expect(res.body.map(function (item){return item.id})).to.not.be(id)
        done()
      })
  })
})
after(function(){
  process.exit()
})